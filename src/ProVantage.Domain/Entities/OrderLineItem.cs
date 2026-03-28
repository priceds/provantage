using ProVantage.Domain.Common;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class OrderLineItem : AuditableEntity
{
    public Guid PurchaseOrderId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public Money UnitPrice { get; set; } = Money.Zero();
    public Money TotalPrice => new(QuantityOrdered * UnitPrice.Amount, UnitPrice.Currency);

    public bool IsFullyReceived => QuantityReceived >= QuantityOrdered;

    // Navigation
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
}
