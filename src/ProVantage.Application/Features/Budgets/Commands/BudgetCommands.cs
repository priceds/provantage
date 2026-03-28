using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Budgets.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Features.Budgets.Commands;

// ═══════════════════════════════════════════
// ALLOCATE BUDGET (create or update)
// ═══════════════════════════════════════════
public record AllocateBudgetCommand(AllocateBudgetRequest Data) : ICommand<Result<Guid>>;

public class AllocateBudgetValidator : AbstractValidator<AllocateBudgetCommand>
{
    public AllocateBudgetValidator()
    {
        RuleFor(x => x.Data.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.FiscalYear).InclusiveBetween(2020, 2099);
        RuleFor(x => x.Data.AllocatedAmount).GreaterThan(0);
        RuleFor(x => x.Data.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Data.FiscalQuarter).InclusiveBetween(1, 4)
            .When(x => x.Data.Period == BudgetPeriod.Quarterly);
        RuleFor(x => x.Data.FiscalMonth).InclusiveBetween(1, 12)
            .When(x => x.Data.Period == BudgetPeriod.Monthly);
    }
}

public class AllocateBudgetHandler : IRequestHandler<AllocateBudgetCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public AllocateBudgetHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(AllocateBudgetCommand request, CancellationToken ct)
    {
        var data = request.Data;

        // Find existing budget for same dept/category/period/year
        var existing = await _db.BudgetAllocations
            .FirstOrDefaultAsync(b =>
                b.TenantId == _tenant.TenantId &&
                b.Department == data.Department &&
                b.Category == data.Category &&
                b.Period == data.Period &&
                b.FiscalYear == data.FiscalYear &&
                b.FiscalQuarter == data.FiscalQuarter &&
                b.FiscalMonth == data.FiscalMonth, ct);

        if (existing is not null)
        {
            // Update allocated amount; preserve committed/spent
            existing.AllocatedAmount = new Money(data.AllocatedAmount, data.Currency);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Success(existing.Id);
        }

        var budget = new BudgetAllocation
        {
            Department = data.Department,
            Category = data.Category,
            Period = data.Period,
            FiscalYear = data.FiscalYear,
            FiscalQuarter = data.FiscalQuarter,
            FiscalMonth = data.FiscalMonth,
            AllocatedAmount = new Money(data.AllocatedAmount, data.Currency),
            CommittedAmount = Money.Zero(data.Currency),
            SpentAmount = Money.Zero(data.Currency),
            TenantId = _tenant.TenantId
        };

        _db.BudgetAllocations.Add(budget);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(budget.Id);
    }
}
