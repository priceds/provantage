using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Notifications.DTOs;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Notifications.Queries;

public record GetNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    bool UnreadOnly = false) : IQuery<PaginatedList<NotificationDto>>;

public class GetNotificationsHandler
    : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public GetNotificationsHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    public async Task<PaginatedList<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken ct)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == _user.UserId)
            .AsNoTracking();

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        var projected = query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type.ToString(),
                n.IsRead,
                n.ActionUrl,
                n.EntityType,
                n.EntityId,
                n.CreatedAt,
                TimeAgo(n.CreatedAt)));

        return await PaginatedList<NotificationDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }

    private static string TimeAgo(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        return diff.TotalMinutes < 1 ? "just now"
            : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago"
            : diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago"
            : $"{(int)diff.TotalDays}d ago";
    }
}
