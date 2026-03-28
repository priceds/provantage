using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class Contract : AuditableEntity
{
    public string ContractNumber { get; set; } = string.Empty; // CON-2026-00001
    public string Title { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public DateRange Duration { get; set; } = new();
    public Money TotalValue { get; set; } = Money.Zero();
    public string Terms { get; set; } = string.Empty;
    public string? DocumentUrl { get; set; }
    public DateTime? RenewedAt { get; set; }

    public bool IsExpiringSoon(int daysThreshold = 30) =>
        Status == ContractStatus.Active && Duration.DaysRemaining <= daysThreshold;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
}
