using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Auth.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Auth.Handlers;

public class LoginHandler : IRequestHandler<Commands.LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginHandler(IApplicationDbContext db, ITokenService tokenService, IPasswordHasher passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthResponse>> Handle(
        Commands.LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);

        if (!user.IsActive)
            return Result<AuthResponse>.Failure("Account is deactivated.", 403);

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role, user.TenantId, user.Department);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken, refreshToken, DateTime.UtcNow.AddHours(1),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName,
                user.Role, user.TenantId, user.Tenant.Name)));
    }
}

public class RegisterHandler : IRequestHandler<Commands.RegisterCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterHandler(IApplicationDbContext db, ITokenService tokenService, IPasswordHasher passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthResponse>> Handle(
        Commands.RegisterCommand request, CancellationToken ct)
    {
        var tenant = new Tenant
        {
            Name = request.TenantName,
            Subdomain = request.TenantSubdomain.ToLowerInvariant()
        };
        _db.Tenants.Add(tenant);

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Admin",
            Department = "Administration",
            TenantId = tenant.Id,
            CreatedBy = "system"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role, tenant.Id, user.Department);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken, refreshToken, DateTime.UtcNow.AddHours(1),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName,
                user.Role, tenant.Id, tenant.Name)));
    }
}

public class RevokeTokenHandler : IRequestHandler<Commands.RevokeTokenCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RevokeTokenHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(Commands.RevokeTokenCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);

        if (user is null)
            return Result.Failure("Invalid refresh token.", 400);

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = DateTime.MinValue;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

public class RefreshTokenHandler : IRequestHandler<Commands.RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokenService;

    public RefreshTokenHandler(IApplicationDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(
        Commands.RefreshTokenCommand request, CancellationToken ct)
    {
        var principal = _tokenService.ValidateExpiredToken(request.AccessToken);
        if (principal is null)
            return Result<AuthResponse>.Failure("Invalid access token.", 401);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var id))
            return Result<AuthResponse>.Failure("Invalid token claims.", 401);

        var user = await _db.Users.Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null || user.RefreshToken != request.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result<AuthResponse>.Failure("Invalid or expired refresh token.", 401);

        var newAccess = _tokenService.GenerateAccessToken(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role, user.TenantId, user.Department);
        var newRefresh = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            newAccess, newRefresh, DateTime.UtcNow.AddHours(1),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName,
                user.Role, user.TenantId, user.Tenant.Name)));
    }
}
