using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Events;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Entities;

public class PurchaseRequisition : AuditableEntity
{
    public string RequisitionNumber { get; set; } = string.Empty; // Auto-generated: PR-2026-00001
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime RequiredByDate { get; set; }
    public Guid RequestedById { get; set; }
    public Guid? ApprovedById { get; set; }
    public Guid? PreferredVendorId { get; set; }

    // Calculated
    public Money TotalAmount => LineItems
        .Aggregate(Money.Zero(), (sum, item) => sum.Add(item.TotalPrice));

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
    public User? ApprovedBy { get; set; }
    public Vendor? PreferredVendor { get; set; }
    public ICollection<RequisitionLineItem> LineItems { get; set; } = [];
    public ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = [];

    // Domain behavior
    public void Submit()
    {
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException("Only draft requisitions can be submitted.");

        if (!LineItems.Any())
            throw new InvalidOperationException("Requisition must have at least one line item.");

        Status = RequisitionStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        AddDomainEvent(new RequisitionSubmittedEvent(Id, TenantId, TotalAmount.Amount));
    }

    public void Approve(Guid approverId)
    {
        if (Status != RequisitionStatus.Submitted && Status != RequisitionStatus.UnderReview)
            throw new InvalidOperationException("Requisition is not in a reviewable state.");

        Status = RequisitionStatus.Approved;
        ApprovedById = approverId;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != RequisitionStatus.Submitted && Status != RequisitionStatus.UnderReview)
            throw new InvalidOperationException("Requisition is not in a reviewable state.");

        Status = RequisitionStatus.Rejected;
        RejectionReason = reason;
    }
}
