using Microsoft.AspNetCore.SignalR;

namespace ProVantage.Infrastructure.SignalR;

/// <summary>
/// SignalR hub for real-time user notifications.
/// Users join a group based on their tenant+user ID.
/// </summary>
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// SignalR hub for real-time dashboard KPI updates.
/// All users in a tenant receive dashboard refreshes.
/// </summary>
public class DashboardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard_{tenantId}");

        await base.OnConnectedAsync();
    }
}
