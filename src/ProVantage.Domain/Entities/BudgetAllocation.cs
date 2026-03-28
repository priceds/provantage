using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class BudgetAllocation : AuditableEntity
{
    public string Department { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public BudgetPeriod Period { get; set; }
    public int FiscalYear { get; set; }
    public int FiscalQuarter { get; set; } // 1-4, relevant for Quarterly period
    public int FiscalMonth { get; set; }   // 1-12, relevant for Monthly period
    public Money AllocatedAmount { get; set; } = Money.Zero();
    public Money CommittedAmount { get; set; } = Money.Zero(); // In approved POs
    public Money SpentAmount { get; set; } = Money.Zero();     // In paid invoices

    public Money AvailableAmount => AllocatedAmount
        .Subtract(CommittedAmount)
        .Subtract(SpentAmount);

    public decimal UtilizationPercent => AllocatedAmount.Amount > 0
        ? Math.Round((CommittedAmount.Amount + SpentAmount.Amount) / AllocatedAmount.Amount * 100, 2)
        : 0;

    public bool IsOverBudget => AvailableAmount.Amount < 0;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
