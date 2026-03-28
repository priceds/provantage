using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.PurchaseOrders.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Features.PurchaseOrders.Commands;

// ═══════════════════════════════════════════
// CREATE PURCHASE ORDER
// ═══════════════════════════════════════════
public record CreatePurchaseOrderCommand(CreatePurchaseOrderRequest Data) : ICommand<Result<Guid>>;

public class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(x => x.Data.VendorId).NotEmpty();
        RuleFor(x => x.Data.ExpectedDeliveryDate).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.Data.ShippingAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Data.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Data.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.ItemDescription).NotEmpty();
            li.RuleFor(x => x.QuantityOrdered).GreaterThan(0);
            li.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });
    }
}

public class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreatePurchaseOrderHandler(
        IApplicationDbContext db, ICurrentTenantService tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var data = request.Data;

        // Validate vendor exists and is approved
        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(v => v.Id == data.VendorId && v.TenantId == _tenant.TenantId, ct);
        if (vendor is null) return Result<Guid>.NotFound("Vendor not found.");
        if (vendor.Status != VendorStatus.Approved)
            return Result<Guid>.Failure("Only approved vendors can receive purchase orders.");

        // If from a requisition, validate and mark it as ConvertedToOrder
        if (data.RequisitionId.HasValue)
        {
            var req = await _db.PurchaseRequisitions
                .FirstOrDefaultAsync(r => r.Id == data.RequisitionId.Value && r.TenantId == _tenant.TenantId, ct);
            if (req is null) return Result<Guid>.NotFound("Requisition not found.");
            if (req.Status != RequisitionStatus.Approved)
                return Result<Guid>.Failure("Only approved requisitions can be converted to purchase orders.");
            req.Status = RequisitionStatus.ConvertedToOrder;
        }

        // Generate PO number
        var count = await _db.PurchaseOrders
            .Where(p => p.TenantId == _tenant.TenantId)
            .CountAsync(ct);
        var orderNumber = $"PO-{DateTime.UtcNow:yyyy}-{(count + 1):D5}";

        var po = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            VendorId = data.VendorId,
            RequisitionId = data.RequisitionId,
            Status = OrderStatus.Created,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = data.ExpectedDeliveryDate,
            PaymentTerms = data.PaymentTerms,
            ShippingAddress = data.ShippingAddress,
            Notes = data.Notes,
            TenantId = _tenant.TenantId
        };

        foreach (var li in data.LineItems)
        {
            po.LineItems.Add(new OrderLineItem
            {
                ItemDescription = li.ItemDescription,
                ItemCode = li.ItemCode,
                UnitOfMeasure = li.UnitOfMeasure,
                QuantityOrdered = li.QuantityOrdered,
                UnitPrice = new Money(li.UnitPrice, li.Currency),
                TenantId = _tenant.TenantId
            });
        }

        _db.PurchaseOrders.Add(po);

        // Update budget committed amount
        var currency = data.LineItems.FirstOrDefault()?.Currency ?? "USD";
        var totalAmount = data.LineItems.Sum(li => li.QuantityOrdered * li.UnitPrice);
        await UpdateBudgetCommitment(totalAmount, currency, ct);

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(po.Id);
    }

    private async Task UpdateBudgetCommitment(decimal amount, string currency, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var budget = await _db.BudgetAllocations
            .Where(b => b.TenantId == _tenant.TenantId
                && b.FiscalYear == now.Year
                && b.Period == Domain.Enums.BudgetPeriod.Annual)
            .FirstOrDefaultAsync(ct);

        if (budget is not null)
        {
            // Use budget's currency to keep Money operations consistent
            var budgetCurrency = budget.AllocatedAmount.Currency;
            budget.CommittedAmount = new Money(budget.CommittedAmount.Amount + amount, budgetCurrency);

            if (budget.UtilizationPercent > 80)
            {
                budget.AddDomainEvent(new Domain.Events.BudgetThresholdExceededEvent(
                    budget.Id, budget.TenantId, budget.Department, budget.UtilizationPercent));
            }
        }
    }
}

// ═══════════════════════════════════════════
// UPDATE ORDER STATUS
// ═══════════════════════════════════════════
public record UpdateOrderStatusCommand(Guid Id, OrderStatus Status) : ICommand<Result>;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public UpdateOrderStatusHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == _tenant.TenantId, ct);

        if (po is null) return Result.NotFound("Purchase order not found.");

        // Validate state transitions
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Created] = [OrderStatus.Sent, OrderStatus.Cancelled],
            [OrderStatus.Sent] = [OrderStatus.Acknowledged, OrderStatus.Cancelled],
            [OrderStatus.Acknowledged] = [OrderStatus.PartiallyReceived, OrderStatus.Received],
            [OrderStatus.PartiallyReceived] = [OrderStatus.Received, OrderStatus.Closed],
            [OrderStatus.Received] = [OrderStatus.Closed],
        };

        if (validTransitions.TryGetValue(po.Status, out var allowed) && !allowed.Contains(request.Status))
            return Result.Failure($"Cannot transition from {po.Status} to {request.Status}.");

        po.Status = request.Status;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
