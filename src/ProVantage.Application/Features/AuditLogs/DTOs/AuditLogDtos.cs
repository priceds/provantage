namespace ProVantage.Application.Features.AuditLogs.DTOs;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    string PerformedBy,
    DateTime Timestamp);
