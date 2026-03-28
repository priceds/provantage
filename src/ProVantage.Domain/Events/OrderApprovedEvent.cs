using ProVantage.Domain.Common;

namespace ProVantage.Domain.Events;

public sealed record OrderApprovedEvent(
    Guid OrderId,
    Guid VendorId,
    Guid TenantId) : IDomainEvent;
