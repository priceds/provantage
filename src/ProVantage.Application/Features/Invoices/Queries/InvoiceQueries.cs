using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Invoices.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Invoices.Queries;

// ═══════════════════════════════════════════
// GET INVOICES (paginated)
// ═══════════════════════════════════════════
public record GetInvoicesQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    InvoiceStatus? Status = null,
    Guid? PurchaseOrderId = null) : IQuery<PaginatedList<InvoiceDto>>;

public class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, PaginatedList<InvoiceDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetInvoicesHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedList<InvoiceDto>> Handle(
        GetInvoicesQuery request, CancellationToken ct)
    {
        var query = _db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.LineItems)
            .Where(i => i.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(i =>
                i.InvoiceNumber.Contains(request.Search) ||
                i.InternalReference.Contains(request.Search) ||
                i.Vendor.CompanyName.Contains(request.Search));

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.PurchaseOrderId.HasValue)
            query = query.Where(i => i.PurchaseOrderId == request.PurchaseOrderId.Value);

        var projected = query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.InternalReference,
                i.PurchaseOrderId,
                i.PurchaseOrder.OrderNumber,
                i.Vendor.CompanyName,
                i.Status,
                i.InvoiceDate,
                i.DueDate,
                i.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount),
                i.LineItems.Any() ? i.LineItems.First().UnitPrice.Currency : "USD",
                i.MatchedAt,
                i.CreatedAt));

        return await PaginatedList<InvoiceDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

// ═══════════════════════════════════════════
// GET INVOICE BY ID
// ═══════════════════════════════════════════
public record GetInvoiceByIdQuery(Guid Id) : IQuery<Result<InvoiceDetailDto>>;

public class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetInvoiceByIdHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<InvoiceDetailDto>> Handle(
        GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var i = await _db.Invoices
            .Include(x => x.Vendor)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.LineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == _tenant.TenantId, ct);

        if (i is null) return Result<InvoiceDetailDto>.NotFound("Invoice not found.");

        var currency = i.LineItems.Any() ? i.LineItems.First().UnitPrice.Currency : "USD";

        var dto = new InvoiceDetailDto(
            i.Id,
            i.InvoiceNumber,
            i.InternalReference,
            i.PurchaseOrderId,
            i.PurchaseOrder.OrderNumber,
            i.VendorId,
            i.Vendor.CompanyName,
            i.Status,
            i.InvoiceDate,
            i.DueDate,
            i.DisputeNotes,
            i.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount),
            currency,
            i.MatchedAt,
            i.PaidAt,
            i.CreatedAt,
            i.LineItems.Select(li => new InvoiceLineItemDto(
                li.Id,
                li.ItemDescription,
                li.ItemCode,
                li.Quantity,
                li.UnitPrice.Amount,
                li.UnitPrice.Currency,
                li.Quantity * li.UnitPrice.Amount)).ToList());

        return Result<InvoiceDetailDto>.Success(dto);
    }
}
