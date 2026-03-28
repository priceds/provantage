using ProVantage.Domain.Enums;

namespace ProVantage.Application.Features.Requisitions.DTOs;

public record RequisitionDto(
    Guid Id,
    string RequisitionNumber,
    string Title,
    string Department,
    RequisitionStatus Status,
    string RequestedByName,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? ApprovedAt);

public record RequisitionDetailDto(
    Guid Id,
    string RequisitionNumber,
    string Title,
    string Description,
    string Department,
    RequisitionStatus Status,
    string? RejectionReason,
    string RequestedByName,
    string? ApprovedByName,
    Guid? PreferredVendorId,
    string? PreferredVendorName,
    List<LineItemDto> LineItems,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt);

public record LineItemDto(
    Guid Id, string ItemDescription, string ItemCode,
    string Category, decimal Quantity, string UnitOfMeasure,
    decimal UnitPrice, string Currency, decimal TotalPrice);

public record CreateRequisitionRequest(
    string Title,
    string Description,
    string Department,
    Guid? PreferredVendorId,
    string Currency,
    List<CreateLineItemRequest> LineItems);

public record CreateLineItemRequest(
    string ItemDescription, string ItemCode, string Category,
    decimal Quantity, string UnitOfMeasure, decimal UnitPrice);
