using ProVantage.Domain.Enums;

namespace ProVantage.Application.Features.Budgets.DTOs;

public record BudgetAllocationDto(
    Guid Id,
    string Department,
    string Category,
    BudgetPeriod Period,
    int FiscalYear,
    int FiscalQuarter,
    int FiscalMonth,
    decimal AllocatedAmount,
    decimal CommittedAmount,
    decimal SpentAmount,
    decimal AvailableAmount,
    decimal UtilizationPercent,
    bool IsOverBudget,
    string Currency);

public record AllocateBudgetRequest(
    string Department,
    string Category,
    BudgetPeriod Period,
    int FiscalYear,
    int FiscalQuarter,
    int FiscalMonth,
    decimal AllocatedAmount,
    string Currency);
