using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Infrastructure.Persistence;
using ProVantage.Infrastructure.SignalR;

namespace ProVantage.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — runs every hour.
/// Finds POs past their expected delivery date that are still open,
/// creates a notification for the requisition originator and broadcasts
/// a dashboard refresh signal to the tenant.
/// </summary>
public class SlaEscalationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaEscalationJob> _logger;

    public SlaEscalationJob(IServiceScopeFactory scopeFactory, ILogger<SlaEscalationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var dashboardHub = scope.ServiceProvider.GetRequiredService<IHubContext<DashboardHub>>();

        var overduePOs = await db.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.Requisition)
            .Where(po => (po.Status == OrderStatus.Sent || po.Status == OrderStatus.Acknowledged)
                      && po.ExpectedDeliveryDate < DateTime.UtcNow)
            .ToListAsync();

        _logger.LogInformation("SlaEscalationJob: found {Count} overdue POs", overduePOs.Count);

        var notifiedTenants = new HashSet<Guid>();

        foreach (var po in overduePOs)
        {
            var daysOverdue = (int)(DateTime.UtcNow - po.ExpectedDeliveryDate).TotalDays;

            // Notify the requisition originator (or PO creator) once per day
            // by checking if a notification was already sent today
            var alreadyNotified = await db.Notifications
                .AnyAsync(n => n.EntityId == po.Id
                            && n.EntityType == "PurchaseOrder"
                            && n.CreatedAt >= DateTime.UtcNow.Date);

            if (alreadyNotified) continue;

            // Find the user to notify — requisition requester or PO creator
            Guid? notifyUserId = null;
            if (po.Requisition is not null)
            {
                notifyUserId = po.Requisition.RequestedById;
            }
            else
            {
                var creator = await db.Users
                    .FirstOrDefaultAsync(u => u.Email == po.CreatedBy && u.TenantId == po.TenantId);
                notifyUserId = creator?.Id;
            }

            if (notifyUserId.HasValue)
            {
                await notificationService.SendToUserAsync(
                    notifyUserId.Value,
                    po.TenantId,
                    "Delivery SLA Exceeded",
                    $"PO {po.OrderNumber} from {po.Vendor.CompanyName} is {daysOverdue} day(s) overdue.",
                    NotificationType.Warning,
                    actionUrl: $"/purchase-orders/{po.Id}",
                    entityType: "PurchaseOrder",
                    entityId: po.Id);
            }

            notifiedTenants.Add(po.TenantId);
        }

        // Broadcast dashboard refresh to each affected tenant
        foreach (var tenantId in notifiedTenants)
        {
            await dashboardHub.Clients
                .Group($"dashboard_{tenantId}")
                .SendAsync("DashboardRefresh");
        }

        _logger.LogInformation("SlaEscalationJob completed");
    }
}
