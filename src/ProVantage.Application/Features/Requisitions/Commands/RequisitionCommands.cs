using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Requisitions.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Features.Requisitions.Commands;

// ═══════════════════════════════════════════
// CREATE REQUISITION (Draft)
// ═══════════════════════════════════════════
public record CreateRequisitionCommand(CreateRequisitionRequest Data) : ICommand<Result<Guid>>;

public class CreateRequisitionValidator : AbstractValidator<CreateRequisitionCommand>
{
    public CreateRequisitionValidator()
    {
        RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Data.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Data.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Data.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.ItemDescription).NotEmpty();
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });
    }
}

public class CreateRequisitionHandler : IRequestHandler<CreateRequisitionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICurrentTenantService _tenant;

    public CreateRequisitionHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, ICurrentTenantService tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(CreateRequisitionCommand request, CancellationToken ct)
    {
        var data = request.Data;

        // Generate sequential number
        var count = await _db.PurchaseRequisitions
            .Where(r => r.TenantId == _tenant.TenantId)
            .CountAsync(ct);
        var reqNumber = $"PR-{DateTime.UtcNow:yyyy}-{(count + 1):D5}";

        var requisition = new PurchaseRequisition
        {
            RequisitionNumber = reqNumber,
            Title = data.Title,
            Description = data.Description,
            Department = data.Department,
            RequestedById = _currentUser.UserId,
            PreferredVendorId = data.PreferredVendorId,
            TenantId = _tenant.TenantId,
            Status = RequisitionStatus.Draft
        };

        foreach (var li in data.LineItems)
        {
            requisition.LineItems.Add(new RequisitionLineItem
            {
                ItemDescription = li.ItemDescription,
                ItemCode = li.ItemCode,
                Category = li.Category,
                Quantity = li.Quantity,
                UnitOfMeasure = li.UnitOfMeasure,
                UnitPrice = new Money(li.UnitPrice, data.Currency),
                TenantId = _tenant.TenantId
            });
        }

        _db.PurchaseRequisitions.Add(requisition);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(requisition.Id);
    }
}

// ═══════════════════════════════════════════
// SUBMIT REQUISITION (for approval)
// ═══════════════════════════════════════════
public record SubmitRequisitionCommand(Guid Id) : ICommand<Result>;

public class SubmitRequisitionHandler : IRequestHandler<SubmitRequisitionCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public SubmitRequisitionHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result> Handle(SubmitRequisitionCommand request, CancellationToken ct)
    {
        var requisition = await _db.PurchaseRequisitions
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == _tenant.TenantId, ct);

        if (requisition is null) return Result.NotFound("Requisition not found.");

        try
        {
            requisition.Submit(); // Domain behavior — validates state + raises event
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        // Load tenant thresholds
        var tenantSettings = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);

        var totalAmount = requisition.LineItems.Sum(li => li.Quantity * li.UnitPrice.Amount);
        var autoThreshold = tenantSettings?.AutoApproveThreshold ?? 5000m;
        var managerThreshold = tenantSettings?.ManagerApprovalThreshold ?? 50000m;

        // Auto-approve if below threshold
        if (totalAmount <= autoThreshold)
        {
            requisition.Approve(Guid.Empty); // system auto-approval
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        // Build threshold-based approval workflow
        var workflow = new ApprovalWorkflow
        {
            RequisitionId = requisition.Id,
            TenantId = _tenant.TenantId,
            CurrentStepOrder = 1
        };

        if (totalAmount <= managerThreshold)
        {
            // Single manager approval
            var manager = await _db.Users
                .Where(u => u.TenantId == _tenant.TenantId && u.Role == "Manager" && u.IsActive)
                .FirstOrDefaultAsync(ct)
                ?? await _db.Users
                    .Where(u => u.TenantId == _tenant.TenantId && u.Role == "Admin" && u.IsActive)
                    .FirstOrDefaultAsync(ct);

            if (manager is not null)
            {
                workflow.Steps.Add(new ApprovalStep
                {
                    StepOrder = 1,
                    ApproverId = manager.Id,
                    DueDate = DateTime.UtcNow.AddDays(2),
                    TenantId = _tenant.TenantId
                });
            }
        }
        else
        {
            // Two-level: manager then director/admin
            var manager = await _db.Users
                .Where(u => u.TenantId == _tenant.TenantId && u.Role == "Manager" && u.IsActive)
                .FirstOrDefaultAsync(ct);

            var director = await _db.Users
                .Where(u => u.TenantId == _tenant.TenantId && u.Role == "Director" && u.IsActive)
                .FirstOrDefaultAsync(ct)
                ?? await _db.Users
                    .Where(u => u.TenantId == _tenant.TenantId && u.Role == "Admin" && u.IsActive)
                    .FirstOrDefaultAsync(ct);

            int stepOrder = 1;
            if (manager is not null)
            {
                workflow.Steps.Add(new ApprovalStep
                {
                    StepOrder = stepOrder++,
                    ApproverId = manager.Id,
                    DueDate = DateTime.UtcNow.AddDays(2),
                    TenantId = _tenant.TenantId
                });
            }
            if (director is not null)
            {
                workflow.Steps.Add(new ApprovalStep
                {
                    StepOrder = stepOrder,
                    ApproverId = director.Id,
                    DueDate = DateTime.UtcNow.AddDays(4),
                    TenantId = _tenant.TenantId
                });
            }
        }

        if (workflow.Steps.Any())
            _db.ApprovalWorkflows.Add(workflow);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════════════════════
// APPROVE / REJECT REQUISITION
// ═══════════════════════════════════════════
public record ApproveRequisitionCommand(Guid Id, string? Comments) : ICommand<Result>;

public class ApproveRequisitionHandler : IRequestHandler<ApproveRequisitionCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICurrentTenantService _tenant;

    public ApproveRequisitionHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, ICurrentTenantService tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<Result> Handle(ApproveRequisitionCommand request, CancellationToken ct)
    {
        var requisition = await _db.PurchaseRequisitions
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == _tenant.TenantId, ct);

        if (requisition is null) return Result.NotFound("Requisition not found.");

        try
        {
            requisition.Approve(_currentUser.UserId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        // Update workflow step
        var workflow = await _db.ApprovalWorkflows
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.RequisitionId == request.Id, ct);

        if (workflow is not null)
        {
            var step = workflow.Steps
                .FirstOrDefault(s => s.ApproverId == _currentUser.UserId && s.Action == null);
            if (step is not null)
            {
                step.Action = ApprovalAction.Approve;
                step.ActionDate = DateTime.UtcNow;
                step.Comments = request.Comments;
            }
            workflow.IsCompleted = true;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record RejectRequisitionCommand(Guid Id, string Reason) : ICommand<Result>;

public class RejectRequisitionHandler : IRequestHandler<RejectRequisitionCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public RejectRequisitionHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result> Handle(RejectRequisitionCommand request, CancellationToken ct)
    {
        var requisition = await _db.PurchaseRequisitions
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == _tenant.TenantId, ct);

        if (requisition is null) return Result.NotFound("Requisition not found.");

        try { requisition.Reject(request.Reason); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
