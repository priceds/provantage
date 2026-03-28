namespace ProVantage.Domain.Common;

/// <summary>
/// Extends BaseEntity with audit fields populated automatically by the EF interceptor.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public Guid TenantId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? ModifiedBy { get; set; }
}
