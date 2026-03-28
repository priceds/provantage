using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class PurchaseOrder : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty; // PO-2026-00001
    public Guid VendorId { get; set; }
    public Guid? RequisitionId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpectedDeliveryDate { get; set; }
    public string PaymentTerms { get; set; } = "Net 30";
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Money TotalAmount => LineItems
        .Aggregate(Money.Zero(), (sum, item) => sum.Add(item.TotalPrice));

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public PurchaseRequisition? Requisition { get; set; }
    public ICollection<OrderLineItem> LineItems { get; set; } = [];
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}
