namespace ProVantage.Application.Features.Dashboard.DTOs;

public record SpendDataPoint(string Month, decimal Amount, string Currency);

public record PendingApprovalItem(
    Guid Id,
    string Number,
    string Title,
    string Department,
    decimal Amount,
    string Currency,
    DateTime SubmittedAt);

public record RecentActivityItem(
    Guid Id,
    string Text,
    string TimeAgo,
    string Color,
    string Icon,
    DateTime Timestamp);

public record DashboardKpisDto(
    decimal TotalSpendMtd,
    string Currency,
    decimal SpendChangePct,
    int OpenPurchaseOrders,
    int PendingApprovals,
    int ActiveVendors,
    double BudgetUtilizationAvg,
    IReadOnlyList<SpendDataPoint> SpendTrend,
    IReadOnlyList<PendingApprovalItem> PendingApprovalsList,
    IReadOnlyList<RecentActivityItem> RecentActivity);
