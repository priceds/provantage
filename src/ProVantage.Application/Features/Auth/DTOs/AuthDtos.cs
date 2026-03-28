namespace ProVantage.Application.Features.Auth.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid TenantId,
    string TenantName);

public record LoginRequest(string Email, string Password);
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string TenantName,
    string TenantSubdomain);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record RevokeTokenRequest(string RefreshToken);
