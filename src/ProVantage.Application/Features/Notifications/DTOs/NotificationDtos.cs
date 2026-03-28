namespace ProVantage.Application.Features.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    string? ActionUrl,
    string? EntityType,
    Guid? EntityId,
    DateTime CreatedAt,
    string TimeAgo);
