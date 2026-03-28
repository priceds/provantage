namespace ProVantage.Domain.Enums;

public enum ApprovalAction
{
    Approve = 0,
    Reject = 1,
    RequestMoreInfo = 2,
    Escalate = 3,
    Delegate = 4
}

public enum NotificationType
{
    Info = 0,
    Warning = 1,
    Success = 2,
    Error = 3,
    ApprovalRequired = 4
}

public enum AuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    StatusChanged = 3,
    Approved = 4,
    Rejected = 5,
    Submitted = 6,
    Matched = 7
}

public enum ContractStatus
{
    Draft = 0,
    Active = 1,
    Expiring = 2,
    Expired = 3,
    Terminated = 4,
    Renewed = 5
}

public enum BudgetPeriod
{
    Monthly = 0,
    Quarterly = 1,
    Annual = 2
}
