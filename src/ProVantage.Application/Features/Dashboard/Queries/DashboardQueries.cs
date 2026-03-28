using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Dashboard.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Dashboard.Queries;

// ═══════════════════════════════════════════
// GET DASHBOARD KPIs
// ═══════════════════════════════════════════

/// <summary>
/// Returns aggregated KPI data for the current tenant's dashboard.
/// Results are cached per tenant via ICacheService inside the handler.
/// </summary>
public record GetDashboardKpisQuery : IQuery<Result<DashboardKpisDto>>;

public class GetDashboardKpisHandler : IRequestHandler<GetDashboardKpisQuery, Result<DashboardKpisDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly ICacheService _cache;

    public GetDashboardKpisHandler(
        IApplicationDbContext db,
        ICurrentTenantService tenant,
        ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result<DashboardKpisDto>> Handle(GetDashboardKpisQuery request, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        var cacheKey = $"dashboard:{tenantId}";

        var cached = await _cache.GetAsync<DashboardKpisDto>(cacheKey, ct);
        if (cached is not null) return Result<DashboardKpisDto>.Success(cached);

        var dto = await BuildKpisAsync(tenantId, ct);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(1), ct);

        return Result<DashboardKpisDto>.Success(dto);
    }

    private async Task<DashboardKpisDto> BuildKpisAsync(Guid tenantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var mtdStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMtdStart = mtdStart.AddMonths(-1);

        // ── Total spend this month (matched invoices) ──
        var spendMtd = await _db.InvoiceLineItems
            .Where(li => li.Invoice.TenantId == tenantId
                      && li.Invoice.Status == InvoiceStatus.Matched
                      && li.Invoice.InvoiceDate >= mtdStart)
            .SumAsync(li => (decimal?)(li.Quantity * li.UnitPrice.Amount) ?? 0m, ct);

        var spendPrevMtd = await _db.InvoiceLineItems
            .Where(li => li.Invoice.TenantId == tenantId
                      && li.Invoice.Status == InvoiceStatus.Matched
                      && li.Invoice.InvoiceDate >= prevMtdStart
                      && li.Invoice.InvoiceDate < mtdStart)
            .SumAsync(li => (decimal?)(li.Quantity * li.UnitPrice.Amount) ?? 0m, ct);

        var spendChangePct = spendPrevMtd > 0
            ? Math.Round((spendMtd - spendPrevMtd) / spendPrevMtd * 100, 1)
            : 0m;

        // ── Open POs ──
        var openPOs = await _db.PurchaseOrders
            .CountAsync(po => po.TenantId == tenantId
                           && po.Status != OrderStatus.Closed
                           && po.Status != OrderStatus.Cancelled, ct);

        // ── Pending approvals ──
        var pendingApprovals = await _db.PurchaseRequisitions
            .CountAsync(pr => pr.TenantId == tenantId
                           && (pr.Status == RequisitionStatus.Submitted
                            || pr.Status == RequisitionStatus.UnderReview), ct);

        // ── Active vendors ──
        var activeVendors = await _db.Vendors
            .CountAsync(v => v.TenantId == tenantId && v.Status == VendorStatus.Approved, ct);

        // ── Avg budget utilization (in-memory after load — UtilizationPercent is computed) ──
        var budgetUtils = await _db.BudgetAllocations
            .Where(b => b.TenantId == tenantId)
            .ToListAsync(ct);
        var avgUtil = budgetUtils.Count > 0
            ? budgetUtils.Average(b => (double)b.UtilizationPercent)
            : 0d;

        // ── Spend trend (last 6 months) ──
        var trendStart = mtdStart.AddMonths(-5);
        var rawTrend = await _db.InvoiceLineItems
            .Where(li => li.Invoice.TenantId == tenantId
                      && li.Invoice.Status == InvoiceStatus.Matched
                      && li.Invoice.InvoiceDate >= trendStart)
            .GroupBy(li => new { li.Invoice.InvoiceDate.Year, li.Invoice.InvoiceDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(li => li.Quantity * li.UnitPrice.Amount)
            })
            .ToListAsync(ct);

        var trendPoints = new List<SpendDataPoint>();
        for (var i = 5; i >= 0; i--)
        {
            var m = mtdStart.AddMonths(-i);
            var entry = rawTrend.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            trendPoints.Add(new SpendDataPoint(m.ToString("MMM"), entry?.Total ?? 0m, "USD"));
        }

        // ── Pending approvals list (top 5) ──
        var pendingList = await _db.PurchaseRequisitions
            .Include(pr => pr.LineItems)
            .Where(pr => pr.TenantId == tenantId
                      && (pr.Status == RequisitionStatus.Submitted
                       || pr.Status == RequisitionStatus.UnderReview))
            .OrderBy(pr => pr.SubmittedAt)
            .Take(5)
            .ToListAsync(ct);

        var pendingDtos = pendingList.Select(pr => new PendingApprovalItem(
            pr.Id,
            pr.RequisitionNumber,
            pr.Title,
            pr.Department,
            pr.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount),
            pr.LineItems.Any() ? pr.LineItems.First().UnitPrice.Currency : "USD",
            pr.SubmittedAt ?? pr.CreatedAt)).ToList();

        // ── Recent activity (latest POs + matched invoices, merged top 10) ──
        var recentPOs = await _db.PurchaseOrders
            .Include(po => po.Vendor)
            .Where(po => po.TenantId == tenantId)
            .OrderByDescending(po => po.CreatedAt)
            .Take(5)
            .Select(po => new RecentActivityItem(
                po.Id,
                $"{po.OrderNumber} sent to {po.Vendor.CompanyName}",
                TimeAgo(po.CreatedAt),
                "hsl(210, 100%, 64%)",
                "shopping_cart",
                po.CreatedAt))
            .ToListAsync(ct);

        var recentInvoices = await _db.Invoices
            .Include(i => i.Vendor)
            .Where(i => i.TenantId == tenantId && i.Status == InvoiceStatus.Matched)
            .OrderByDescending(i => i.MatchedAt)
            .Take(5)
            .Select(i => new RecentActivityItem(
                i.Id,
                $"Invoice {i.InvoiceNumber} matched successfully",
                TimeAgo(i.MatchedAt ?? i.CreatedAt),
                "hsl(165, 82%, 51%)",
                "check_circle",
                i.MatchedAt ?? i.CreatedAt))
            .ToListAsync(ct);

        var activity = recentPOs
            .Concat(recentInvoices)
            .OrderByDescending(x => x.Timestamp)
            .Take(10)
            .ToList();

        return new DashboardKpisDto(
            spendMtd, "USD", spendChangePct,
            openPOs, pendingApprovals, activeVendors,
            Math.Round(avgUtil, 1),
            trendPoints, pendingDtos, activity);
    }

    private static string TimeAgo(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        return diff.TotalMinutes < 1 ? "just now"
            : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes} min ago"
            : diff.TotalHours < 24 ? $"{(int)diff.TotalHours} hours ago"
            : $"{(int)diff.TotalDays} days ago";
    }
}
