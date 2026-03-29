using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Features.Contracts.Commands;
using ProVantage.Application.Tests.Common.Testing;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Tests.Features.Contracts;

public class CreateContractHandlerTests
{
    [Fact]
    public async Task Handle_returns_failure_when_vendor_is_not_approved()
    {
        var tenant = new TestCurrentTenantService(Guid.NewGuid());
        await using var db = TestApplicationDbContextFactory.CreateContext(tenant);
        var cache = new RecordingCacheService();

        db.Vendors.Add(new Vendor
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            CompanyName = "Pending Vendor",
            Email = "pending@example.com",
            Phone = "1234567890",
            Category = "IT",
            Status = VendorStatus.PendingApproval,
            Address = new Address
            {
                Street = "1 Test St",
                City = "Austin",
                State = "TX",
                PostalCode = "73301",
                Country = "USA"
            }
        });
        await db.SaveChangesAsync();

        var handler = new CreateContractHandler(db, tenant, cache);
        var command = new CreateContractCommand(
            db.Vendors.Single().Id,
            "Pending Vendor Agreement",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(90),
            1250m,
            "USD");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Vendor must exist and be approved before creating a contract.", result.Error);
        Assert.Empty(await db.Contracts.ToListAsync());
        Assert.Empty(cache.RemovedPrefixes);
    }

    [Fact]
    public async Task Handle_creates_contract_and_invalidates_expiring_cache_prefix()
    {
        var tenant = new TestCurrentTenantService(Guid.NewGuid());
        await using var db = TestApplicationDbContextFactory.CreateContext(tenant);
        var cache = new RecordingCacheService();
        var vendorId = Guid.NewGuid();

        db.Vendors.Add(new Vendor
        {
            Id = vendorId,
            TenantId = tenant.TenantId,
            CompanyName = "Northwind Systems",
            Email = "ops@northwind.example",
            Phone = "555-0100",
            Category = "IT",
            Status = VendorStatus.Approved,
            Address = new Address
            {
                Street = "10 Tech Park",
                City = "Seattle",
                State = "WA",
                PostalCode = "98101",
                Country = "USA"
            }
        });
        db.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            VendorId = vendorId,
            ContractNumber = "CTR-OLD-0001",
            Title = "Existing Contract",
            Status = ContractStatus.Active,
            Duration = new DateRange(DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(120)),
            TotalValue = new Money(5000m, "USD"),
            Terms = "Existing terms"
        });
        await db.SaveChangesAsync();

        var handler = new CreateContractHandler(db, tenant, cache);
        var result = await handler.Handle(
            new CreateContractCommand(
                vendorId,
                "  Endpoint Support Plan  ",
                DateTime.UtcNow.Date.AddDays(-1),
                DateTime.UtcNow.Date.AddDays(10),
                24000m,
                "usd"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var contract = await db.Contracts.SingleAsync(c => c.Id == result.Value);
        var tenantFragment = tenant.TenantId.ToString("N")[..4].ToUpperInvariant();

        Assert.Equal("Endpoint Support Plan", contract.Title);
        Assert.Equal("USD", contract.TotalValue.Currency);
        Assert.Equal(ContractStatus.Expiring, contract.Status);
        Assert.Equal($"CTR-{tenantFragment}-{DateTime.UtcNow:yyyyMM}-0002", contract.ContractNumber);
        Assert.Contains($"contracts:expiring:{tenant.TenantId:N}", cache.RemovedPrefixes);
    }
}
