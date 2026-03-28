using ProVantage.Domain.Common;

namespace ProVantage.Domain.Entities;

public class GoodsReceipt : AuditableEntity
{
    public string ReceiptNumber { get; set; } = string.Empty; // GR-2026-00001
    public Guid PurchaseOrderId { get; set; }
    public Guid ReceivedById { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string? DeliveryNote { get; set; }

    // Line-level receipt tracking
    public string ItemCode { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public decimal QuantityRejected { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public User ReceivedBy { get; set; } = null!;
}
