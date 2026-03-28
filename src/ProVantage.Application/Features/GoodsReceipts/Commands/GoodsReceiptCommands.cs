using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.GoodsReceipts.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.GoodsReceipts.Commands;

// ═══════════════════════════════════════════
// CREATE GOODS RECEIPT
// ═══════════════════════════════════════════
public record CreateGoodsReceiptCommand(CreateGoodsReceiptRequest Data) : ICommand<Result<Guid>>;

public class CreateGoodsReceiptValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptValidator()
    {
        RuleFor(x => x.Data.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Data.ItemCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.QuantityReceived).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.QuantityRejected).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data).Must(d => d.QuantityReceived + d.QuantityRejected > 0)
            .WithMessage("Total received + rejected must be greater than zero.");
    }
}

public class CreateGoodsReceiptHandler : IRequestHandler<CreateGoodsReceiptCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateGoodsReceiptHandler(
        IApplicationDbContext db, ICurrentTenantService tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateGoodsReceiptCommand request, CancellationToken ct)
    {
        var data = request.Data;

        var po = await _db.PurchaseOrders
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == data.PurchaseOrderId && p.TenantId == _tenant.TenantId, ct);

        if (po is null) return Result<Guid>.NotFound("Purchase order not found.");

        if (po.Status == OrderStatus.Cancelled || po.Status == OrderStatus.Closed)
            return Result<Guid>.Failure($"Cannot record goods receipt for a {po.Status} order.");

        // Validate item code exists on the PO
        var lineItem = po.LineItems.FirstOrDefault(li => li.ItemCode == data.ItemCode);
        if (lineItem is null)
            return Result<Guid>.Failure($"Item code '{data.ItemCode}' not found on this purchase order.");

        // Generate GR number
        var count = await _db.GoodsReceipts
            .Where(g => g.TenantId == _tenant.TenantId)
            .CountAsync(ct);
        var receiptNumber = $"GR-{DateTime.UtcNow:yyyy}-{(count + 1):D5}";

        var receipt = new GoodsReceipt
        {
            ReceiptNumber = receiptNumber,
            PurchaseOrderId = data.PurchaseOrderId,
            ReceivedById = _currentUser.UserId,
            ReceivedDate = DateTime.UtcNow,
            ItemCode = data.ItemCode,
            QuantityReceived = data.QuantityReceived,
            QuantityRejected = data.QuantityRejected,
            RejectionReason = data.RejectionReason,
            DeliveryNote = data.DeliveryNote,
            Notes = data.Notes,
            TenantId = _tenant.TenantId
        };

        _db.GoodsReceipts.Add(receipt);

        // Update OrderLineItem.QuantityReceived
        lineItem.QuantityReceived += data.QuantityReceived;

        // Recalculate PO status
        await UpdatePoStatus(po, ct);

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(receipt.Id);
    }

    private static Task UpdatePoStatus(PurchaseOrder po, CancellationToken ct)
    {
        if (!po.LineItems.Any()) return Task.CompletedTask;

        var allReceived = po.LineItems.All(li => li.IsFullyReceived);
        var anyReceived = po.LineItems.Any(li => li.QuantityReceived > 0);

        po.Status = allReceived
            ? OrderStatus.Received
            : anyReceived
                ? OrderStatus.PartiallyReceived
                : po.Status;

        return Task.CompletedTask;
    }
}
