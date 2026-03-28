using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.PurchaseOrders.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.PurchaseOrders.Queries;

// ═══════════════════════════════════════════
// GET PURCHASE ORDERS (paginated)
// ═══════════════════════════════════════════
public record GetPurchaseOrdersQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    OrderStatus? Status = null,
    Guid? VendorId = null) : IQuery<PaginatedList<PurchaseOrderDto>>;

public class GetPurchaseOrdersHandler
    : IRequestHandler<GetPurchaseOrdersQuery, PaginatedList<PurchaseOrderDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetPurchaseOrdersHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedList<PurchaseOrderDto>> Handle(
        GetPurchaseOrdersQuery request, CancellationToken ct)
    {
        var query = _db.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.LineItems)
            .Include(p => p.Requisition)
            .Where(p => p.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p =>
                p.OrderNumber.Contains(request.Search) ||
                p.Vendor.CompanyName.Contains(request.Search));

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.VendorId.HasValue)
            query = query.Where(p => p.VendorId == request.VendorId.Value);

        var projected = query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PurchaseOrderDto(
                p.Id,
                p.OrderNumber,
                p.Vendor.CompanyName,
                p.VendorId,
                p.RequisitionId,
                p.Requisition != null ? p.Requisition.RequisitionNumber : null,
                p.Status,
                p.OrderDate,
                p.ExpectedDeliveryDate,
                p.LineItems.Sum(li => li.QuantityOrdered * li.UnitPrice.Amount),
                p.LineItems.Any() ? p.LineItems.First().UnitPrice.Currency : "USD",
                p.PaymentTerms,
                p.CreatedAt));

        return await PaginatedList<PurchaseOrderDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

// ═══════════════════════════════════════════
// GET PURCHASE ORDER BY ID
// ═══════════════════════════════════════════
public record GetPurchaseOrderByIdQuery(Guid Id) : IQuery<Result<PurchaseOrderDetailDto>>;

public class GetPurchaseOrderByIdHandler
    : IRequestHandler<GetPurchaseOrderByIdQuery, Result<PurchaseOrderDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetPurchaseOrderByIdHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<PurchaseOrderDetailDto>> Handle(
        GetPurchaseOrderByIdQuery request, CancellationToken ct)
    {
        var p = await _db.PurchaseOrders
            .Include(x => x.Vendor)
            .Include(x => x.LineItems)
            .Include(x => x.Requisition)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == _tenant.TenantId, ct);

        if (p is null) return Result<PurchaseOrderDetailDto>.NotFound("Purchase order not found.");

        var currency = p.LineItems.Any() ? p.LineItems.First().UnitPrice.Currency : "USD";

        var dto = new PurchaseOrderDetailDto(
            p.Id,
            p.OrderNumber,
            p.VendorId,
            p.Vendor.CompanyName,
            p.RequisitionId,
            p.Requisition?.RequisitionNumber,
            p.Status,
            p.OrderDate,
            p.ExpectedDeliveryDate,
            p.PaymentTerms,
            p.ShippingAddress,
            p.Notes,
            p.LineItems.Sum(li => li.QuantityOrdered * li.UnitPrice.Amount),
            currency,
            p.CreatedAt,
            p.LineItems.Select(li => new OrderLineItemDto(
                li.Id,
                li.ItemDescription,
                li.ItemCode,
                li.UnitOfMeasure,
                li.QuantityOrdered,
                li.QuantityReceived,
                li.UnitPrice.Amount,
                li.UnitPrice.Currency,
                li.QuantityOrdered * li.UnitPrice.Amount,
                li.IsFullyReceived)).ToList());

        return Result<PurchaseOrderDetailDto>.Success(dto);
    }
}
