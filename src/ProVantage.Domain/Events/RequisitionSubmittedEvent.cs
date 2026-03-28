using ProVantage.Domain.Common;

namespace ProVantage.Domain.Events;

public sealed record RequisitionSubmittedEvent(
    Guid RequisitionId,
    Guid TenantId,
    decimal TotalAmount) : IDomainEvent;
