using ProVantage.Domain.Common;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class InvoiceLineItem : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Money UnitPrice { get; set; } = Money.Zero();
    public Money TotalPrice => new(Quantity * UnitPrice.Amount, UnitPrice.Currency);

    // Navigation
    public Invoice Invoice { get; set; } = null!;
}
