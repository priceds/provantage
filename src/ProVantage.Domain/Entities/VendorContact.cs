using ProVantage.Domain.Common;

namespace ProVantage.Domain.Entities;

public class VendorContact : AuditableEntity
{
    public Guid VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }

    // Navigation
    public Vendor Vendor { get; set; } = null!;
}
