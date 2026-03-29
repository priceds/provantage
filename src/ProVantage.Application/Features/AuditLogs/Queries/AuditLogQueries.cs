using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.AuditLogs.DTOs;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.AuditLogs.Queries;

public record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    string? EntityId = null,
    DateTime? From = null,
    DateTime? To = null) : IQuery<Result<PaginatedList<AuditLogDto>>>;

public class GetAuditLogsHandler
    : IRequestHandler<GetAuditLogsQuery, Result<PaginatedList<AuditLogDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetAuditLogsHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<PaginatedList<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = _db.AuditLogs
            .Where(a => a.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim();
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            if (!Guid.TryParse(request.EntityId, out var entityId))
            {
                return Result<PaginatedList<AuditLogDto>>.Failure("EntityId must be a valid GUID.");
            }

            query = query.Where(a => a.EntityId == entityId);
        }

        if (request.From.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            var inclusiveEnd = request.To.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < inclusiveEnd);
        }

        var projected = query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(
                a.Id,
                a.EntityType,
                a.EntityId.ToString(),
                a.Action.ToString(),
                a.OldValues,
                a.NewValues,
                string.IsNullOrWhiteSpace(a.UserName) ? a.UserId.ToString() : a.UserName,
                a.CreatedAt));

        var paged = await PaginatedList<AuditLogDto>.CreateAsync(
            projected, request.Page, request.PageSize, ct);

        return Result<PaginatedList<AuditLogDto>>.Success(paged);
    }
}
