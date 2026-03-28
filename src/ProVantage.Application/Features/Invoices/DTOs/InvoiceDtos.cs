using ProVantage.Domain.Enums;

namespace ProVantage.Application.Features.Invoices.DTOs;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string InternalReference,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    string VendorName,
    InvoiceStatus Status,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    string Currency,
    DateTime? MatchedAt,
    DateTime CreatedAt);

public record InvoiceLineItemDto(
    Guid Id,
    string ItemDescription,
    string ItemCode,
    decimal Quantity,
    decimal UnitPrice,
    string Currency,
    decimal TotalPrice);

public record ThreeWayMatchLineResult(
    string ItemCode,
    string ItemDescription,
    decimal InvoiceQty,
    decimal InvoiceUnitPrice,
    decimal PoQty,
    decimal PoUnitPrice,
    decimal ReceivedQty,
    decimal PriceVariancePercent,
    decimal QuantityVariancePercent,
    bool IsPriceMatched,
    bool IsQuantityMatched,
    bool IsMatched,
    string? DiscrepancyNote);

public record ThreeWayMatchResultDto(
    Guid InvoiceId,
    InvoiceStatus ResultStatus,
    bool IsFullyMatched,
    string Summary,
    List<ThreeWayMatchLineResult> LineResults);

public record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    string InternalReference,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid VendorId,
    string VendorName,
    InvoiceStatus Status,
    DateTime InvoiceDate,
    DateTime DueDate,
    string? DisputeNotes,
    decimal TotalAmount,
    string Currency,
    DateTime? MatchedAt,
    DateTime? PaidAt,
    DateTime CreatedAt,
    List<InvoiceLineItemDto> LineItems);

public record CreateInvoiceRequest(
    string InvoiceNumber,
    Guid PurchaseOrderId,
    DateTime InvoiceDate,
    DateTime DueDate,
    List<CreateInvoiceLineItemRequest> LineItems);

public record CreateInvoiceLineItemRequest(
    string ItemDescription,
    string ItemCode,
    decimal Quantity,
    decimal UnitPrice,
    string Currency);
