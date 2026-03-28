using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Events;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class Invoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty; // Vendor's invoice number
    public string InternalReference { get; set; } = string.Empty; // INV-2026-00001
    public Guid PurchaseOrderId { get; set; }
    public Guid VendorId { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? DisputeNotes { get; set; }
    public DateTime? MatchedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public Money TotalAmount => LineItems
        .Aggregate(Money.Zero(), (sum, item) => sum.Add(item.TotalPrice));

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = [];

    // Domain behavior
    public void MarkAsMatched()
    {
        Status = InvoiceStatus.Matched;
        MatchedAt = DateTime.UtcNow;
        AddDomainEvent(new InvoiceMatchedEvent(Id, PurchaseOrderId, TenantId));
    }

    public void MarkAsDisputed(string notes)
    {
        Status = InvoiceStatus.Disputed;
        DisputeNotes = notes;
    }
}
