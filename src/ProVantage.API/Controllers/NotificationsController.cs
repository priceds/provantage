using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProVantage.API.Controllers;
using ProVantage.Application.Features.Notifications.Commands;
using ProVantage.Application.Features.Notifications.Queries;

namespace ProVantage.API.Controllers;

[Authorize]
[Route("api/notifications")]
public class NotificationsController : BaseApiController
{
    /// <summary>Returns the current user's notifications (paginated).</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetNotificationsQuery(page, pageSize, unreadOnly), ct);
        return Ok(result);
    }

    /// <summary>Marks a single notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new MarkNotificationReadCommand(id), ct);
        return HandleResult(result);
    }

    /// <summary>Marks all unread notifications as read for the current user.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var result = await Mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return HandleResult(result);
    }
}
