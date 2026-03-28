namespace ProVantage.Application.Features.Analytics.DTOs;

public record SpendByCategoryDto(
    string Category,
    string Department,
    decimal TotalSpend,
    string Currency,
    int InvoiceCount);

public record VendorPerformanceDto(
    Guid VendorId,
    string VendorName,
    int TotalOrders,
    int CompletedOrders,
    decimal OnTimeDeliveryRate,
    int TotalInvoices,
    int MatchedInvoices,
    decimal InvoiceMatchRate,
    decimal TotalSpend,
    string Currency);
