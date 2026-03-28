namespace ProVantage.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}

public interface ICurrentTenantService
{
    Guid TenantId { get; }
    string TenantName { get; }
    void SetTenant(Guid tenantId, string tenantName);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(string fileName, Stream content, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string firstName, string lastName,
        string role, Guid tenantId, string department);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? ValidateExpiredToken(string token);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface INotificationService
{
    /// <summary>Persists a notification to DB and pushes it to the user via SignalR.</summary>
    Task SendToUserAsync(Guid userId, Guid tenantId, string title, string message,
        ProVantage.Domain.Enums.NotificationType type,
        string? actionUrl = null, string? entityType = null, Guid? entityId = null,
        CancellationToken ct = default);

    /// <summary>Broadcasts a real-time push to all users in a tenant (no DB persistence).</summary>
    Task SendToTenantAsync(Guid tenantId, string title, string message,
        ProVantage.Domain.Enums.NotificationType type,
        CancellationToken ct = default);
}
