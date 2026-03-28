using ProVantage.Domain.Common;

namespace ProVantage.Domain.Events;

public sealed record InvoiceMatchedEvent(
    Guid InvoiceId,
    Guid PurchaseOrderId,
    Guid TenantId) : IDomainEvent;
