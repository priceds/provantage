using ProVantage.Application.Features.AuditLogs.Queries;
using ProVantage.Application.Tests.Common.Testing;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;

namespace ProVantage.Application.Tests.Features.AuditLogs;

public class GetAuditLogsHandlerTests
{
    [Fact]
    public async Task Handle_returns_failure_for_invalid_entity_id_filter()
    {
        var tenant = new TestCurrentTenantService(Guid.NewGuid());
        await using var db = TestApplicationDbContextFactory.CreateContext(tenant);
        var handler = new GetAuditLogsHandler(db, tenant);

        var result = await handler.Handle(
            new GetAuditLogsQuery(EntityId: "not-a-guid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EntityId must be a valid GUID.", result.Error);
    }

    [Fact]
    public async Task Handle_filters_by_entity_type_date_range_and_tenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var tenant = new TestCurrentTenantService(tenantId);
        var entityId = Guid.NewGuid();

        await using var db = TestApplicationDbContextFactory.CreateContext(tenant);
        db.AuditLogs.AddRange(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                UserName = "admin@test.com",
                Action = AuditAction.Created,
                EntityType = "Vendor",
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow.AddHours(-4),
                NewValues = "{\"companyName\":\"Northwind\"}"
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                UserName = "admin@test.com",
                Action = AuditAction.Updated,
                EntityType = "Vendor",
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                OldValues = "{\"status\":\"PendingApproval\"}",
                NewValues = "{\"status\":\"Approved\"}"
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                UserName = "admin@test.com",
                Action = AuditAction.Created,
                EntityType = "Invoice",
                EntityId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                NewValues = "{\"invoiceNumber\":\"INV-001\"}"
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = otherTenantId,
                UserId = Guid.NewGuid(),
                UserName = "other@test.com",
                Action = AuditAction.Created,
                EntityType = "Vendor",
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                NewValues = "{\"companyName\":\"Other Tenant Vendor\"}"
            });
        await db.SaveChangesAsync();

        var handler = new GetAuditLogsHandler(db, tenant);
        var result = await handler.Handle(
            new GetAuditLogsQuery(
                EntityType: "Vendor",
                From: DateTime.UtcNow.AddDays(-1),
                To: DateTime.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.All(result.Value.Items, item => Assert.Equal("Vendor", item.EntityType));
        Assert.Equal("Updated", result.Value.Items[0].Action);
        Assert.Equal(entityId.ToString(), result.Value.Items[0].EntityId);
        Assert.All(result.Value.Items, item => Assert.Equal("admin@test.com", item.PerformedBy));
    }
}
