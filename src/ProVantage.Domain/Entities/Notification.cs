using ProVantage.Domain.Common;
using ProVantage.Domain.Enums;

namespace ProVantage.Domain.Entities;

public class Notification : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; } // Deep link into the SPA
    public string? EntityType { get; set; } // "PurchaseRequisition", "Invoice", etc.
    public Guid? EntityId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
