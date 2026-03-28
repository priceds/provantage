using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;

namespace ProVantage.Domain.Entities;

public class ApprovalWorkflow : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public int CurrentStepOrder { get; set; } = 1;
    public bool IsCompleted { get; set; }

    // Navigation
    public PurchaseRequisition Requisition { get; set; } = null!;
    public ICollection<ApprovalStep> Steps { get; set; } = [];
}

public class ApprovalStep : AuditableEntity
{
    public Guid WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public Guid ApproverId { get; set; }
    public ApprovalAction? Action { get; set; }
    public string? Comments { get; set; }
    public DateTime? ActionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsEscalated { get; set; }

    public bool IsPending => Action is null && !IsEscalated;

    // Navigation
    public ApprovalWorkflow Workflow { get; set; } = null!;
    public User Approver { get; set; } = null!;
}
