using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Infrastructure.SignalR;

namespace ProVantage.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext db,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(
        Guid userId, Guid tenantId, string title, string message,
        NotificationType type, string? actionUrl = null,
        string? entityType = null, Guid? entityId = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            TenantId = tenantId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            EntityType = entityType,
            EntityId = entityId
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        var payload = new
        {
            id = notification.Id,
            title,
            message,
            type = type.ToString(),
            actionUrl,
            createdAt = notification.CreatedAt
        };

        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", payload, ct);

        _logger.LogDebug("Notification sent to user {UserId}: {Title}", userId, title);
    }

    public async Task SendToTenantAsync(
        Guid tenantId, string title, string message,
        NotificationType type,
        CancellationToken ct = default)
    {
        var payload = new { title, message, type = type.ToString() };

        await _hubContext.Clients
            .Group($"tenant_{tenantId}")
            .SendAsync("ReceiveNotification", payload, ct);

        _logger.LogDebug("Broadcast notification to tenant {TenantId}: {Title}", tenantId, title);
    }
}
