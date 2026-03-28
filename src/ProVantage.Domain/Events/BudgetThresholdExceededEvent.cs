using ProVantage.Domain.Common;

namespace ProVantage.Domain.Events;

public sealed record BudgetThresholdExceededEvent(
    Guid BudgetAllocationId,
    Guid TenantId,
    string Department,
    decimal UtilizationPercent) : IDomainEvent;
