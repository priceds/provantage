using ProVantage.Domain.Enums;

namespace ProVantage.Application.Features.PurchaseOrders.DTOs;

public record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    string VendorName,
    Guid VendorId,
    Guid? RequisitionId,
    string? RequisitionNumber,
    OrderStatus Status,
    DateTime OrderDate,
    DateTime ExpectedDeliveryDate,
    decimal TotalAmount,
    string Currency,
    string PaymentTerms,
    DateTime CreatedAt);

public record OrderLineItemDto(
    Guid Id,
    string ItemDescription,
    string ItemCode,
    string UnitOfMeasure,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal UnitPrice,
    string Currency,
    decimal TotalPrice,
    bool IsFullyReceived);

public record PurchaseOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid VendorId,
    string VendorName,
    Guid? RequisitionId,
    string? RequisitionNumber,
    OrderStatus Status,
    DateTime OrderDate,
    DateTime ExpectedDeliveryDate,
    string PaymentTerms,
    string ShippingAddress,
    string? Notes,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    List<OrderLineItemDto> LineItems);

public record CreatePurchaseOrderRequest(
    Guid? RequisitionId,
    Guid VendorId,
    DateTime ExpectedDeliveryDate,
    string PaymentTerms,
    string ShippingAddress,
    string? Notes,
    List<CreateOrderLineItemRequest> LineItems);

public record CreateOrderLineItemRequest(
    string ItemDescription,
    string ItemCode,
    string UnitOfMeasure,
    decimal QuantityOrdered,
    decimal UnitPrice,
    string Currency);

public record UpdateOrderStatusRequest(OrderStatus Status);
