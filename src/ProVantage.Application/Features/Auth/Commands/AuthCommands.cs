using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Auth.DTOs;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Auth.Commands;

// ═══════════════════════════════════════════
// LOGIN
// ═══════════════════════════════════════════
public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

// ═══════════════════════════════════════════
// REGISTER (creates a new tenant + admin user)
// ═══════════════════════════════════════════
public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string TenantName,
    string TenantSubdomain) : IRequest<Result<AuthResponse>>;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    private readonly IApplicationDbContext _db;

    public RegisterValidator(IApplicationDbContext db)
    {
        _db = db;

        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TenantSubdomain).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9-]+$").WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens.")
            .MustAsync(async (subdomain, ct) =>
                !await _db.Tenants.AnyAsync(t => t.Subdomain == subdomain, ct))
            .WithMessage("Subdomain is already taken.");
    }
}

// ═══════════════════════════════════════════
// REFRESH TOKEN
// ═══════════════════════════════════════════
public record RefreshTokenCommand(string AccessToken, string RefreshToken)
    : IRequest<Result<AuthResponse>>;

// ═══════════════════════════════════════════
// REVOKE TOKEN (logout)
// ═══════════════════════════════════════════
public record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;
