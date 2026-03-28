using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.GoodsReceipts.DTOs;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.GoodsReceipts.Queries;

// ═══════════════════════════════════════════
// GET GOODS RECEIPTS BY PO ID
// ═══════════════════════════════════════════
public record GetGoodsReceiptsQuery(
    Guid? PurchaseOrderId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedList<GoodsReceiptDto>>;

public class GetGoodsReceiptsHandler
    : IRequestHandler<GetGoodsReceiptsQuery, PaginatedList<GoodsReceiptDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public GetGoodsReceiptsHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedList<GoodsReceiptDto>> Handle(
        GetGoodsReceiptsQuery request, CancellationToken ct)
    {
        var query = _db.GoodsReceipts
            .Include(g => g.PurchaseOrder)
            .Include(g => g.ReceivedBy)
            .Where(g => g.TenantId == _tenant.TenantId)
            .AsNoTracking();

        if (request.PurchaseOrderId.HasValue)
            query = query.Where(g => g.PurchaseOrderId == request.PurchaseOrderId.Value);

        var projected = query
            .OrderByDescending(g => g.ReceivedDate)
            .Select(g => new GoodsReceiptDto(
                g.Id,
                g.ReceiptNumber,
                g.PurchaseOrderId,
                g.PurchaseOrder.OrderNumber,
                g.ReceivedBy.FirstName + " " + g.ReceivedBy.LastName,
                g.ReceivedDate,
                g.ItemCode,
                g.QuantityReceived,
                g.QuantityRejected,
                g.RejectionReason,
                g.DeliveryNote,
                g.Notes,
                g.CreatedAt));

        return await PaginatedList<GoodsReceiptDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}
