namespace ProVantage.Application.Features.GoodsReceipts.DTOs;

public record GoodsReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    string ReceivedByName,
    DateTime ReceivedDate,
    string ItemCode,
    decimal QuantityReceived,
    decimal QuantityRejected,
    string? RejectionReason,
    string? DeliveryNote,
    string? Notes,
    DateTime CreatedAt);

public record CreateGoodsReceiptRequest(
    Guid PurchaseOrderId,
    string ItemCode,
    decimal QuantityReceived,
    decimal QuantityRejected,
    string? RejectionReason,
    string? DeliveryNote,
    string? Notes);
