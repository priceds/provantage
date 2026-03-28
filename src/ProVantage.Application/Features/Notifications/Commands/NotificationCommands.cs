using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Features.Notifications.Commands;

// ═══════════════════════════════════════════
// MARK SINGLE NOTIFICATION READ
// ═══════════════════════════════════════════
public record MarkNotificationReadCommand(Guid Id) : ICommand<Result>;

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public MarkNotificationReadHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _user.UserId, ct);

        if (notification is null)
            return Result.NotFound("Notification not found.");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// ═══════════════════════════════════════════
// MARK ALL NOTIFICATIONS READ
// ═══════════════════════════════════════════
public record MarkAllNotificationsReadCommand : ICommand<Result>;

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public MarkAllNotificationsReadHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        await _db.Notifications
            .Where(n => n.UserId == _user.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);

        return Result.Success();
    }
}
