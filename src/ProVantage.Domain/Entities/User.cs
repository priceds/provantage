using ProVantage.Domain.Common;

namespace ProVantage.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Buyer"; // Admin, Manager, Buyer, Viewer
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? AvatarUrl { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
