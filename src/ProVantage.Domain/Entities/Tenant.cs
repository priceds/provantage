using ProVantage.Domain.Common;

namespace ProVantage.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string PrimaryCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;

    // Approval thresholds
    public decimal AutoApproveThreshold { get; set; } = 5000m;
    public decimal ManagerApprovalThreshold { get; set; } = 50000m;

    // Three-way match tolerance
    public decimal PriceVarianceTolerancePercent { get; set; } = 5m;
    public decimal QuantityVarianceTolerancePercent { get; set; } = 2m;

    // Navigation
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Vendor> Vendors { get; set; } = [];
}
