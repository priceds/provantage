using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Analytics.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Analytics.Queries;

// ═══════════════════════════════════════════
// SPEND BY CATEGORY
// ═══════════════════════════════════════════
public record GetSpendByCategoryQuery(int Year) : IQuery<Result<List<SpendByCategoryDto>>>, ICacheable
{
    public string CacheKey => $"analytics:spend-by-category:{Year}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

public class GetSpendByCategoryHandler
    : IRequestHandler<GetSpendByCategoryQuery, Result<List<SpendByCategoryDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetSpendByCategoryHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<List<SpendByCategoryDto>>> Handle(
        GetSpendByCategoryQuery request, CancellationToken ct)
    {
        var yearStart = new DateTime(request.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        var rows = await _db.InvoiceLineItems
            .Where(li => li.Invoice.TenantId == _tenant.TenantId
                      && li.Invoice.Status == InvoiceStatus.Matched
                      && li.Invoice.InvoiceDate >= yearStart
                      && li.Invoice.InvoiceDate < yearEnd)
            .GroupBy(li => li.ItemDescription)
            .Select(g => new
            {
                Category = g.Key,
                TotalSpend = g.Sum(li => li.Quantity * li.UnitPrice.Amount),
                Currency = g.First().UnitPrice.Currency,
                InvoiceCount = g.Select(li => li.InvoiceId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalSpend)
            .Take(20)
            .ToListAsync(ct);

        var result = rows
            .Select(r => new SpendByCategoryDto(r.Category, "General", r.TotalSpend, r.Currency, r.InvoiceCount))
            .ToList();

        return Result<List<SpendByCategoryDto>>.Success(result);
    }
}

// ═══════════════════════════════════════════
// VENDOR PERFORMANCE
// ═══════════════════════════════════════════
public record GetVendorPerformanceQuery(int Year) : IQuery<Result<List<VendorPerformanceDto>>>, ICacheable
{
    public string CacheKey => $"analytics:vendor-performance:{Year}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

public class GetVendorPerformanceHandler
    : IRequestHandler<GetVendorPerformanceQuery, Result<List<VendorPerformanceDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetVendorPerformanceHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<List<VendorPerformanceDto>>> Handle(
        GetVendorPerformanceQuery request, CancellationToken ct)
    {
        var yearStart = new DateTime(request.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);
        var tenantId = _tenant.TenantId;

        var vendors = await _db.Vendors
            .Where(v => v.TenantId == tenantId && v.Status == VendorStatus.Approved)
            .Include(v => v.PurchaseOrders.Where(po => po.OrderDate >= yearStart && po.OrderDate < yearEnd))
                .ThenInclude(po => po.GoodsReceipts)
            .AsNoTracking()
            .ToListAsync(ct);

        var vendorIds = vendors.Select(v => v.Id).ToList();

        var invoices = await _db.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.TenantId == tenantId
                     && vendorIds.Contains(i.VendorId)
                     && i.InvoiceDate >= yearStart
                     && i.InvoiceDate < yearEnd)
            .AsNoTracking()
            .ToListAsync(ct);

        var invoicesByVendor = invoices
            .GroupBy(i => i.VendorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = vendors
            .Where(v => v.PurchaseOrders.Any() || invoicesByVendor.ContainsKey(v.Id))
            .Select(v =>
            {
                var orders = v.PurchaseOrders.ToList();
                var completedOrders = orders
                    .Where(po => po.Status == OrderStatus.Received || po.Status == OrderStatus.Closed)
                    .ToList();

                var onTime = completedOrders.Count(po =>
                {
                    var lastReceipt = po.GoodsReceipts
                        .Select(gr => gr.ReceivedDate)
                        .DefaultIfEmpty(DateTime.MinValue)
                        .Max();
                    return lastReceipt <= po.ExpectedDeliveryDate;
                });

                var onTimeRate = completedOrders.Count > 0
                    ? Math.Round((decimal)onTime / completedOrders.Count * 100, 1)
                    : 0m;

                var vendorInvoices = invoicesByVendor.GetValueOrDefault(v.Id, []);
                var matchedInvoices = vendorInvoices.Count(i => i.Status == InvoiceStatus.Matched);
                var matchRate = vendorInvoices.Count > 0
                    ? Math.Round((decimal)matchedInvoices / vendorInvoices.Count * 100, 1)
                    : 0m;

                var totalSpend = vendorInvoices
                    .Where(i => i.Status == InvoiceStatus.Matched)
                    .SelectMany(i => i.LineItems)
                    .Sum(li => li.Quantity * li.UnitPrice.Amount);

                var currency = vendorInvoices
                    .SelectMany(i => i.LineItems)
                    .FirstOrDefault()?.UnitPrice.Currency ?? "USD";

                return new VendorPerformanceDto(
                    v.Id, v.CompanyName,
                    orders.Count, completedOrders.Count,
                    onTimeRate,
                    vendorInvoices.Count, matchedInvoices, matchRate,
                    totalSpend, currency);
            })
            .OrderByDescending(x => x.TotalSpend)
            .ToList();

        return Result<List<VendorPerformanceDto>>.Success(result);
    }
}
