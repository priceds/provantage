using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class Vendor : AuditableEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // IT, Office Supplies, Professional Services, etc.
    public VendorStatus Status { get; set; } = VendorStatus.PendingApproval;
    public string? StatusNotes { get; set; }
    public Address Address { get; set; } = new();
    public string PaymentTerms { get; set; } = "Net 30";
    public decimal Rating { get; set; } // 0-5 composite score

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<VendorContact> Contacts { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
