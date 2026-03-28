using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Requisitions.DTOs;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Requisitions.Queries;

// ═══════════════════════════════════════════
// GET PENDING APPROVALS (for current user's queue)
// ═══════════════════════════════════════════
public record PendingApprovalDto(
    Guid RequisitionId,
    string RequisitionNumber,
    string Title,
    string Department,
    string RequestedByName,
    decimal TotalAmount,
    string Currency,
    DateTime SubmittedAt,
    DateTime? DueDate,
    int StepOrder);

public record GetPendingApprovalsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PaginatedList<PendingApprovalDto>>;

public class GetPendingApprovalsHandler
    : IRequestHandler<GetPendingApprovalsQuery, PaginatedList<PendingApprovalDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICurrentTenantService _tenant;

    public GetPendingApprovalsHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ICurrentTenantService tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<PaginatedList<PendingApprovalDto>> Handle(
        GetPendingApprovalsQuery request, CancellationToken ct)
    {
        var query = _db.ApprovalSteps
            .Where(s =>
                s.TenantId == _tenant.TenantId &&
                s.ApproverId == _currentUser.UserId &&
                s.Action == null) // not yet actioned
            .Include(s => s.Workflow)
                .ThenInclude(w => w.Requisition)
                    .ThenInclude(r => r.RequestedBy)
            .Include(s => s.Workflow)
                .ThenInclude(w => w.Requisition)
                    .ThenInclude(r => r.LineItems)
            .Where(s => s.StepOrder == s.Workflow.CurrentStepOrder) // only active step
            .AsNoTracking();

        var projected = query
            .OrderBy(s => s.DueDate)
            .Select(s => new PendingApprovalDto(
                s.Workflow.RequisitionId,
                s.Workflow.Requisition.RequisitionNumber,
                s.Workflow.Requisition.Title,
                s.Workflow.Requisition.Department,
                s.Workflow.Requisition.RequestedBy.FirstName + " " + s.Workflow.Requisition.RequestedBy.LastName,
                s.Workflow.Requisition.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount),
                s.Workflow.Requisition.LineItems.Any()
                    ? s.Workflow.Requisition.LineItems.First().UnitPrice.Currency
                    : "USD",
                s.Workflow.Requisition.SubmittedAt ?? s.Workflow.Requisition.CreatedAt,
                s.DueDate,
                s.StepOrder));

        return await PaginatedList<PendingApprovalDto>.CreateAsync(
            projected, request.Page, request.PageSize, ct);
    }
}
