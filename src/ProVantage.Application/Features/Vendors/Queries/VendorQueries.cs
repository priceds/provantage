using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Vendors.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Vendors.Queries;

// ═══════════════════════════════════════════
// GET VENDORS (paginated, filterable)
// ═══════════════════════════════════════════
public record GetVendorsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    VendorStatus? Status = null,
    string? Category = null) : IQuery<PaginatedList<VendorDto>>;

public class GetVendorsHandler : IRequestHandler<GetVendorsQuery, PaginatedList<VendorDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetVendorsHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedList<VendorDto>> Handle(GetVendorsQuery request, CancellationToken ct)
    {
        var query = _db.Vendors
            .Where(v => v.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(v =>
                v.CompanyName.Contains(request.Search) ||
                v.Email.Contains(request.Search) ||
                v.Category.Contains(request.Search));

        if (request.Status.HasValue)
            query = query.Where(v => v.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(v => v.Category == request.Category);

        var projected = query
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VendorDto(
                v.Id, v.CompanyName, v.Email, v.Phone, v.Category,
                v.Status, v.PaymentTerms, v.Rating,
                v.Address.City, v.Address.Country, v.CreatedAt));

        return await PaginatedList<VendorDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

// ═══════════════════════════════════════════
// GET VENDOR BY ID
// ═══════════════════════════════════════════
public record GetVendorByIdQuery(Guid Id) : IQuery<Result<VendorDetailDto>>;

public class GetVendorByIdHandler : IRequestHandler<GetVendorByIdQuery, Result<VendorDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetVendorByIdHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<VendorDetailDto>> Handle(GetVendorByIdQuery request, CancellationToken ct)
    {
        var vendor = await _db.Vendors
            .Include(v => v.Contacts.Where(c => !c.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.Id && v.TenantId == _tenant.TenantId, ct);

        if (vendor is null) return Result<VendorDetailDto>.NotFound("Vendor not found.");

        var dto = new VendorDetailDto(
            vendor.Id, vendor.CompanyName, vendor.TaxId, vendor.Email, vendor.Phone,
            vendor.Website, vendor.Category, vendor.Status, vendor.StatusNotes,
            vendor.PaymentTerms, vendor.Rating,
            new AddressDto(vendor.Address.Street, vendor.Address.City, vendor.Address.State,
                vendor.Address.PostalCode, vendor.Address.Country),
            vendor.Contacts.Select(c => new VendorContactDto(
                c.Id, c.Name, c.Email, c.Phone, c.JobTitle, c.IsPrimary)).ToList(),
            vendor.CreatedAt, vendor.CreatedBy);

        return Result<VendorDetailDto>.Success(dto);
    }
}
