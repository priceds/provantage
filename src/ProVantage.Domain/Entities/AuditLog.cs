using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;

namespace ProVantage.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValues { get; set; } // JSON snapshot
    public string? NewValues { get; set; } // JSON snapshot
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
