using Microsoft.EntityFrameworkCore;
using ProVantage.Domain.Interfaces;
using ProVantage.Infrastructure.Persistence;

namespace ProVantage.Application.Tests.Common.Testing;

public static class TestApplicationDbContextFactory
{
    public static ApplicationDbContext CreateContext(
        ICurrentTenantService tenantService,
        string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options, tenantService);
    }
}

public sealed class TestCurrentTenantService : ICurrentTenantService
{
    public TestCurrentTenantService(Guid tenantId, string tenantName = "Test Tenant")
    {
        TenantId = tenantId;
        TenantName = tenantName;
    }

    public Guid TenantId { get; private set; }
    public string TenantName { get; private set; }

    public void SetTenant(Guid tenantId, string tenantName)
    {
        TenantId = tenantId;
        TenantName = tenantName;
    }
}

public sealed class RecordingCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);

    public List<string> RemovedKeys { get; } = [];
    public List<string> RemovedPrefixes { get; } = [];

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var value) && value is T typed)
        {
            return Task.FromResult<T?>(typed);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        RemovedKeys.Add(key);
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        RemovedPrefixes.Add(prefix);

        foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            _store.Remove(key);
        }

        return Task.CompletedTask;
    }
}
