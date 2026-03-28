using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Budgets.DTOs;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Budgets.Queries;

// ═══════════════════════════════════════════
// GET BUDGET UTILIZATION
// ═══════════════════════════════════════════
public record GetBudgetUtilizationQuery(
    int FiscalYear,
    BudgetPeriod? Period = null,
    string? Department = null) : IQuery<List<BudgetAllocationDto>>;

public class GetBudgetUtilizationHandler
    : IRequestHandler<GetBudgetUtilizationQuery, List<BudgetAllocationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetBudgetUtilizationHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<BudgetAllocationDto>> Handle(
        GetBudgetUtilizationQuery request, CancellationToken ct)
    {
        var query = _db.BudgetAllocations
            .Where(b => b.TenantId == _tenant.TenantId && b.FiscalYear == request.FiscalYear)
            .AsNoTracking();

        if (request.Period.HasValue)
            query = query.Where(b => b.Period == request.Period.Value);

        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(b => b.Department == request.Department);

        var budgets = await query.OrderBy(b => b.Department).ThenBy(b => b.Category).ToListAsync(ct);

        return budgets.Select(b => new BudgetAllocationDto(
            b.Id,
            b.Department,
            b.Category,
            b.Period,
            b.FiscalYear,
            b.FiscalQuarter,
            b.FiscalMonth,
            b.AllocatedAmount.Amount,
            b.CommittedAmount.Amount,
            b.SpentAmount.Amount,
            b.AvailableAmount.Amount,
            b.UtilizationPercent,
            b.IsOverBudget,
            b.AllocatedAmount.Currency)).ToList();
    }
}
