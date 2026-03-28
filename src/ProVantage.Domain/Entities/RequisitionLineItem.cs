using ProVantage.Domain.Common;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class RequisitionLineItem : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "EA"; // EA, BOX, HR, etc.
    public decimal Quantity { get; set; }
    public Money UnitPrice { get; set; } = Money.Zero();
    public Money TotalPrice => new(Quantity * UnitPrice.Amount, UnitPrice.Currency);

    // Navigation
    public PurchaseRequisition Requisition { get; set; } = null!;
}
