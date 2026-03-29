using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Features.Contracts.Commands;

public record CreateContractCommand(
    Guid VendorId,
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    decimal Value,
    string Currency) : ICommand<Result<Guid>>;

public class CreateContractValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after the start date.");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class CreateContractHandler : IRequestHandler<CreateContractCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly ICacheService _cache;

    public CreateContractHandler(
        IApplicationDbContext db,
        ICurrentTenantService tenant,
        ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result<Guid>> Handle(CreateContractCommand request, CancellationToken ct)
    {
        var vendor = await _db.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v =>
                v.Id == request.VendorId &&
                v.TenantId == _tenant.TenantId &&
                v.Status == VendorStatus.Approved, ct);

        if (vendor is null)
        {
            return Result<Guid>.Failure("Vendor must exist and be approved before creating a contract.");
        }

        var sequence = await _db.Contracts
            .CountAsync(c => c.TenantId == _tenant.TenantId, ct) + 1;

        var contract = new Contract
        {
            VendorId = request.VendorId,
            Title = request.Title.Trim(),
            ContractNumber = BuildContractNumber(_tenant.TenantId, sequence),
            Status = ResolveInitialStatus(request.StartDate, request.EndDate),
            Duration = new DateRange(request.StartDate, request.EndDate),
            TotalValue = new Money(request.Value, request.Currency.ToUpperInvariant()),
            Terms = string.Empty
        };

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveByPrefixAsync($"contracts:expiring:{_tenant.TenantId:N}", ct);

        return Result<Guid>.Success(contract.Id);
    }

    private static string BuildContractNumber(Guid tenantId, int sequence)
    {
        var tenantFragment = tenantId.ToString("N")[..4].ToUpperInvariant();
        return $"CTR-{tenantFragment}-{DateTime.UtcNow:yyyyMM}-{sequence:0000}";
    }

    private static ContractStatus ResolveInitialStatus(DateTime startDate, DateTime endDate)
    {
        var today = DateTime.UtcNow.Date;

        if (endDate.Date < today)
        {
            return ContractStatus.Expired;
        }

        if (endDate.Date <= today.AddDays(30))
        {
            return ContractStatus.Expiring;
        }

        return startDate.Date > today ? ContractStatus.Draft : ContractStatus.Active;
    }
}
