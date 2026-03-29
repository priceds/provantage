using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Contracts.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Contracts.Queries;

public record GetContractsQuery(
    int Page = 1,
    int PageSize = 20,
    ContractStatus? Status = null,
    Guid? VendorId = null,
    bool ExpiringWithin30Days = false) : IQuery<Result<PaginatedList<ContractDto>>>;

public class GetContractsHandler
    : IRequestHandler<GetContractsQuery, Result<PaginatedList<ContractDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetContractsHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<PaginatedList<ContractDto>>> Handle(GetContractsQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var expiringThreshold = today.AddDays(30);

        var query = _db.Contracts
            .Include(c => c.Vendor)
            .Where(c => c.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (request.VendorId.HasValue)
        {
            query = query.Where(c => c.VendorId == request.VendorId.Value);
        }

        if (request.ExpiringWithin30Days)
        {
            query = query.Where(c =>
                c.Status != ContractStatus.Terminated &&
                c.Duration.EndDate >= today &&
                c.Duration.EndDate <= expiringThreshold);
        }

        if (request.Status.HasValue)
        {
            query = request.Status.Value switch
            {
                ContractStatus.Expiring => query.Where(c =>
                    c.Status == ContractStatus.Expiring ||
                    (c.Status == ContractStatus.Active &&
                     c.Duration.EndDate >= today &&
                     c.Duration.EndDate <= expiringThreshold)),
                ContractStatus.Expired => query.Where(c =>
                    c.Status == ContractStatus.Expired || c.Duration.EndDate < today),
                _ => query.Where(c => c.Status == request.Status.Value)
            };
        }

        var projected = query
            .OrderBy(c => c.Duration.EndDate)
            .ThenByDescending(c => c.CreatedAt)
            .Select(c => new ContractProjection(
                c.Id,
                c.ContractNumber,
                c.VendorId,
                c.Vendor.CompanyName,
                c.Title,
                c.Status,
                c.Duration.StartDate,
                c.Duration.EndDate,
                c.TotalValue.Amount,
                c.TotalValue.Currency,
                c.CreatedAt));

        var paged = await PaginatedList<ContractProjection>.CreateAsync(
            projected, request.Page, request.PageSize, ct);

        var items = paged.Items
            .Select(c => ContractMapping.MapDto(c, today))
            .ToList();

        return Result<PaginatedList<ContractDto>>.Success(
            new PaginatedList<ContractDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }
}

public record GetContractByIdQuery(Guid Id) : IQuery<Result<ContractDto>>;

public class GetContractByIdHandler : IRequestHandler<GetContractByIdQuery, Result<ContractDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetContractByIdHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<ContractDto>> Handle(GetContractByIdQuery request, CancellationToken ct)
    {
        var contract = await _db.Contracts
            .Include(c => c.Vendor)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == _tenant.TenantId, ct);

        if (contract is null)
        {
            return Result<ContractDto>.NotFound("Contract not found.");
        }

        var dto = new ContractDto(
            contract.Id,
            contract.ContractNumber,
            contract.VendorId,
            contract.Vendor.CompanyName,
            contract.Title,
            ContractMapping.ResolveStatus(contract.Status, contract.Duration.EndDate, DateTime.UtcNow.Date),
            contract.Duration.StartDate,
            contract.Duration.EndDate,
            contract.TotalValue.Amount,
            contract.TotalValue.Currency,
            Math.Max(0, (contract.Duration.EndDate.Date - DateTime.UtcNow.Date).Days),
            contract.CreatedAt);

        return Result<ContractDto>.Success(dto);
    }
}

public record GetExpiringContractsQuery(int DaysAhead = 30) : IQuery<Result<IReadOnlyList<ContractDto>>>;

public class GetExpiringContractsHandler
    : IRequestHandler<GetExpiringContractsQuery, Result<IReadOnlyList<ContractDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly ICacheService _cache;

    public GetExpiringContractsHandler(
        IApplicationDbContext db,
        ICurrentTenantService tenant,
        ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<ContractDto>>> Handle(
        GetExpiringContractsQuery request,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var cacheKey = $"contracts:expiring:{_tenant.TenantId:N}:{request.DaysAhead}";

        var cached = await _cache.GetAsync<List<ContractDto>>(cacheKey, ct);
        if (cached is not null)
        {
            return Result<IReadOnlyList<ContractDto>>.Success(cached);
        }

        var rows = await _db.Contracts
            .Include(c => c.Vendor)
            .Where(c =>
                c.TenantId == _tenant.TenantId &&
                c.Status != ContractStatus.Terminated &&
                c.Duration.EndDate >= today &&
                c.Duration.EndDate <= today.AddDays(request.DaysAhead))
            .OrderBy(c => c.Duration.EndDate)
            .Select(c => new ContractProjection(
                c.Id,
                c.ContractNumber,
                c.VendorId,
                c.Vendor.CompanyName,
                c.Title,
                c.Status,
                c.Duration.StartDate,
                c.Duration.EndDate,
                c.TotalValue.Amount,
                c.TotalValue.Currency,
                c.CreatedAt))
            .ToListAsync(ct);

        var result = rows.Select(c => ContractMapping.MapDto(c, today)).ToList();

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

        return Result<IReadOnlyList<ContractDto>>.Success(result);
    }
}

file sealed record ContractProjection(
    Guid Id,
    string ContractNumber,
    Guid VendorId,
    string VendorName,
    string Title,
    ContractStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    decimal Value,
    string Currency,
    DateTime CreatedAt);

file static class ContractMapping
{
    public static ContractDto MapDto(ContractProjection contract, DateTime today) =>
        new(
            contract.Id,
            contract.ContractNumber,
            contract.VendorId,
            contract.VendorName,
            contract.Title,
            ResolveStatus(contract.Status, contract.EndDate.Date, today),
            contract.StartDate.Date,
            contract.EndDate.Date,
            contract.Value,
            contract.Currency,
            Math.Max(0, (contract.EndDate.Date - today).Days),
            contract.CreatedAt);

    public static ContractStatus ResolveStatus(
        ContractStatus storedStatus,
        DateTime endDate,
        DateTime today)
    {
        if (storedStatus is ContractStatus.Terminated or ContractStatus.Renewed)
        {
            return storedStatus;
        }

        if (endDate.Date < today)
        {
            return ContractStatus.Expired;
        }

        if (endDate.Date <= today.AddDays(30))
        {
            return ContractStatus.Expiring;
        }

        return storedStatus == ContractStatus.Draft ? ContractStatus.Draft : ContractStatus.Active;
    }
}
