using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Invoices.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Features.Invoices.Commands;

// ═══════════════════════════════════════════
// CREATE INVOICE
// ═══════════════════════════════════════════
public record CreateInvoiceCommand(CreateInvoiceRequest Data) : ICommand<Result<Guid>>;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Data.InvoiceNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Data.InvoiceDate).NotEmpty();
        RuleFor(x => x.Data.DueDate).GreaterThan(x => x.Data.InvoiceDate)
            .WithMessage("Due date must be after invoice date.");
        RuleFor(x => x.Data.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Data.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.ItemCode).NotEmpty();
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });
    }
}

public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public CreateInvoiceHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var data = request.Data;

        var po = await _db.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == data.PurchaseOrderId && p.TenantId == _tenant.TenantId, ct);
        if (po is null) return Result<Guid>.NotFound("Purchase order not found.");

        if (po.Status == OrderStatus.Cancelled)
            return Result<Guid>.Failure("Cannot create invoice for a cancelled purchase order.");

        // Check for duplicate invoice number from same vendor
        var duplicate = await _db.Invoices
            .AnyAsync(i => i.InvoiceNumber == data.InvoiceNumber
                && i.VendorId == po.VendorId
                && i.TenantId == _tenant.TenantId, ct);
        if (duplicate)
            return Result<Guid>.Failure($"Invoice number '{data.InvoiceNumber}' already exists for this vendor.");

        // Generate internal reference
        var count = await _db.Invoices
            .Where(i => i.TenantId == _tenant.TenantId)
            .CountAsync(ct);
        var internalRef = $"INV-{DateTime.UtcNow:yyyy}-{(count + 1):D5}";

        var invoice = new Invoice
        {
            InvoiceNumber = data.InvoiceNumber,
            InternalReference = internalRef,
            PurchaseOrderId = data.PurchaseOrderId,
            VendorId = po.VendorId,
            Status = InvoiceStatus.Pending,
            InvoiceDate = data.InvoiceDate,
            DueDate = data.DueDate,
            TenantId = _tenant.TenantId
        };

        foreach (var li in data.LineItems)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                ItemDescription = li.ItemDescription,
                ItemCode = li.ItemCode,
                Quantity = li.Quantity,
                UnitPrice = new Money(li.UnitPrice, li.Currency),
                TenantId = _tenant.TenantId
            });
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(invoice.Id);
    }
}

// ═══════════════════════════════════════════
// PERFORM THREE-WAY MATCH
// ═══════════════════════════════════════════
public record PerformThreeWayMatchCommand(Guid InvoiceId) : ICommand<Result<ThreeWayMatchResultDto>>;

public class PerformThreeWayMatchHandler
    : IRequestHandler<PerformThreeWayMatchCommand, Result<ThreeWayMatchResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public PerformThreeWayMatchHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<ThreeWayMatchResultDto>> Handle(
        PerformThreeWayMatchCommand request, CancellationToken ct)
    {
        // Load invoice with line items
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.PurchaseOrder)
                .ThenInclude(p => p.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.TenantId == _tenant.TenantId, ct);

        if (invoice is null) return Result<ThreeWayMatchResultDto>.NotFound("Invoice not found.");

        if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
            return Result<ThreeWayMatchResultDto>.Failure($"Cannot match a {invoice.Status} invoice.");

        // Load all goods receipts for the PO
        var goodsReceipts = await _db.GoodsReceipts
            .Where(g => g.PurchaseOrderId == invoice.PurchaseOrderId && g.TenantId == _tenant.TenantId)
            .ToListAsync(ct);

        // Get tolerance settings from tenant
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        var priceTolerancePercent = tenant?.PriceVarianceTolerancePercent ?? 5m;
        var qtyTolerancePercent = tenant?.QuantityVarianceTolerancePercent ?? 5m;

        // Build received-qty lookup: ItemCode -> total received
        var receivedByItem = goodsReceipts
            .GroupBy(g => g.ItemCode)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.QuantityReceived));

        var po = invoice.PurchaseOrder;
        var poItemsByCode = po.LineItems.ToDictionary(li => li.ItemCode);

        var lineResults = new List<ThreeWayMatchLineResult>();
        var hasDispute = false;

        foreach (var invLine in invoice.LineItems)
        {
            // Find matching PO line
            if (!poItemsByCode.TryGetValue(invLine.ItemCode, out var poLine))
            {
                lineResults.Add(new ThreeWayMatchLineResult(
                    invLine.ItemCode, invLine.ItemDescription,
                    invLine.Quantity, invLine.UnitPrice.Amount,
                    0, 0, 0,
                    100m, 100m, false, false, false,
                    $"Item code '{invLine.ItemCode}' not found on purchase order."));
                hasDispute = true;
                continue;
            }

            receivedByItem.TryGetValue(invLine.ItemCode, out var receivedQty);

            // Price variance check
            var poPrice = poLine.UnitPrice.Amount;
            var invPrice = invLine.UnitPrice.Amount;
            var priceVariance = poPrice > 0
                ? Math.Abs(invPrice - poPrice) / poPrice * 100m
                : 100m;
            var isPriceMatched = priceVariance <= priceTolerancePercent;

            // Quantity check: invoice qty should not exceed received qty (within tolerance)
            var qtyVariance = receivedQty > 0
                ? Math.Max(0, (invLine.Quantity - receivedQty) / receivedQty * 100m)
                : (invLine.Quantity > 0 ? 100m : 0m);
            var isQtyMatched = invLine.Quantity <= receivedQty * (1 + qtyTolerancePercent / 100m);

            var isLineMatched = isPriceMatched && isQtyMatched;
            if (!isLineMatched) hasDispute = true;

            string? note = null;
            if (!isPriceMatched)
                note = $"Price variance {priceVariance:F1}% exceeds tolerance {priceTolerancePercent}%.";
            else if (!isQtyMatched)
                note = $"Invoice qty {invLine.Quantity} exceeds received qty {receivedQty}.";

            lineResults.Add(new ThreeWayMatchLineResult(
                invLine.ItemCode,
                invLine.ItemDescription,
                invLine.Quantity,
                invPrice,
                poLine.QuantityOrdered,
                poPrice,
                receivedQty,
                Math.Round(priceVariance, 2),
                Math.Round(qtyVariance, 2),
                isPriceMatched,
                isQtyMatched,
                isLineMatched,
                note));
        }

        // Update invoice status
        if (!hasDispute)
        {
            invoice.MarkAsMatched();
            var summary = $"All {lineResults.Count} line item(s) matched within tolerance.";

            // Update budget spent amount
            await UpdateBudgetSpend(invoice, ct);

            await _db.SaveChangesAsync(ct);
            return Result<ThreeWayMatchResultDto>.Success(new ThreeWayMatchResultDto(
                invoice.Id, InvoiceStatus.Matched, true, summary, lineResults));
        }
        else
        {
            var disputedCount = lineResults.Count(l => !l.IsMatched);
            var notes = string.Join("; ", lineResults
                .Where(l => !l.IsMatched && l.DiscrepancyNote != null)
                .Select(l => $"{l.ItemCode}: {l.DiscrepancyNote}"));

            invoice.MarkAsDisputed(notes);
            var summary = $"{disputedCount} of {lineResults.Count} line item(s) have discrepancies.";

            await _db.SaveChangesAsync(ct);
            return Result<ThreeWayMatchResultDto>.Success(new ThreeWayMatchResultDto(
                invoice.Id, InvoiceStatus.Disputed, false, summary, lineResults));
        }
    }

    private async Task UpdateBudgetSpend(Invoice invoice, CancellationToken ct)
    {
        var totalSpend = invoice.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount);

        var now = DateTime.UtcNow;
        var budget = await _db.BudgetAllocations
            .Where(b => b.TenantId == _tenant.TenantId
                && b.FiscalYear == now.Year
                && b.Period == BudgetPeriod.Annual)
            .FirstOrDefaultAsync(ct);

        if (budget is not null)
        {
            var budgetCurrency = budget.AllocatedAmount.Currency;
            budget.SpentAmount = new Money(budget.SpentAmount.Amount + totalSpend, budgetCurrency);
            // Reduce committed since the PO is now invoiced/matched
            var reduce = Math.Min(budget.CommittedAmount.Amount, totalSpend);
            budget.CommittedAmount = new Money(budget.CommittedAmount.Amount - reduce, budgetCurrency);
        }
    }
}
