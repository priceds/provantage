using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Infrastructure.Persistence;

namespace ProVantage.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — runs daily at 08:00 UTC.
/// Finds Active contracts expiring within 30 days and notifies the creator.
/// </summary>
public class ContractExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContractExpiryJob> _logger;

    public ContractExpiryJob(IServiceScopeFactory scopeFactory, ILogger<ContractExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var expiring = await db.Contracts
            .Include(c => c.Vendor)
            .Where(c => c.Status == ContractStatus.Active
                     && c.Duration.EndDate <= DateTime.UtcNow.AddDays(30)
                     && c.Duration.EndDate > DateTime.UtcNow)
            .ToListAsync();

        _logger.LogInformation("ContractExpiryJob: found {Count} expiring contracts", expiring.Count);

        foreach (var contract in expiring)
        {
            var daysLeft = contract.Duration.DaysRemaining;

            // Update status to Expiring if not already
            if (contract.Status == ContractStatus.Active)
            {
                contract.Status = ContractStatus.Expiring;
            }

            // Find the creator user to notify
            var creatorUser = await db.Users
                .FirstOrDefaultAsync(u => u.Email == contract.CreatedBy && u.TenantId == contract.TenantId);

            if (creatorUser is null) continue;

            await notificationService.SendToUserAsync(
                creatorUser.Id,
                contract.TenantId,
                "Contract Expiring Soon",
                $"Contract {contract.ContractNumber} with {contract.Vendor.CompanyName} expires in {daysLeft} days.",
                NotificationType.Warning,
                actionUrl: $"/contracts/{contract.Id}",
                entityType: "Contract",
                entityId: contract.Id);
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("ContractExpiryJob completed");
    }
}
