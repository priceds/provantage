using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Requisitions.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Requisitions.Queries;

// ═══════════════════════════════════════════
// GET REQUISITIONS (paginated)
// ═══════════════════════════════════════════
public record GetRequisitionsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    RequisitionStatus? Status = null,
    string? Department = null) : IQuery<PaginatedList<RequisitionDto>>;

public class GetRequisitionsHandler
    : IRequestHandler<GetRequisitionsQuery, PaginatedList<RequisitionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetRequisitionsHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedList<RequisitionDto>> Handle(
        GetRequisitionsQuery request, CancellationToken ct)
    {
        var query = _db.PurchaseRequisitions
            .Include(r => r.LineItems)
            .Include(r => r.RequestedBy)
            .Where(r => r.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(r =>
                r.Title.Contains(request.Search) ||
                r.RequisitionNumber.Contains(request.Search));

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(r => r.Department == request.Department);

        var projected = query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RequisitionDto(
                r.Id, r.RequisitionNumber, r.Title, r.Department,
                r.Status, r.RequestedBy.FirstName + " " + r.RequestedBy.LastName,
                r.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount),
                r.LineItems.Any() ? r.LineItems.First().UnitPrice.Currency : "USD",
                r.CreatedAt, r.ApprovedAt));

        return await PaginatedList<RequisitionDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

// ═══════════════════════════════════════════
// GET REQUISITION BY ID
// ═══════════════════════════════════════════
public record GetRequisitionByIdQuery(Guid Id) : IQuery<Result<RequisitionDetailDto>>;

public class GetRequisitionByIdHandler
    : IRequestHandler<GetRequisitionByIdQuery, Result<RequisitionDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetRequisitionByIdHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<RequisitionDetailDto>> Handle(
        GetRequisitionByIdQuery request, CancellationToken ct)
    {
        var r = await _db.PurchaseRequisitions
            .Include(x => x.LineItems)
            .Include(x => x.RequestedBy)
            .Include(x => x.ApprovedBy)
            .Include(x => x.PreferredVendor)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == _tenant.TenantId, ct);

        if (r is null) return Result<RequisitionDetailDto>.NotFound("Requisition not found.");

        var dto = new RequisitionDetailDto(
            r.Id, r.RequisitionNumber, r.Title, r.Description, r.Department,
            r.Status, r.RejectionReason,
            r.RequestedBy.FullName,
            r.ApprovedBy?.FullName,
            r.PreferredVendorId, r.PreferredVendor?.CompanyName,
            r.LineItems.Select(li => new LineItemDto(
                li.Id, li.ItemDescription, li.ItemCode, li.Category,
                li.Quantity, li.UnitOfMeasure,
                li.UnitPrice.Amount, li.UnitPrice.Currency,
                li.Quantity * li.UnitPrice.Amount)).ToList(),
            r.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount),
            r.LineItems.Any() ? r.LineItems.First().UnitPrice.Currency : "USD",
            r.CreatedAt, r.SubmittedAt, r.ApprovedAt);

        return Result<RequisitionDetailDto>.Success(dto);
    }
}
