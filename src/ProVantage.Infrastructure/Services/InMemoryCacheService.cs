using System.Collections.Concurrent;
using System.Text.Json;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Infrastructure.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult<T?>(default);
        }

        if (entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(JsonSerializer.Deserialize<T>(entry.Json, JsonOptions));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var expiresAt = expiry.HasValue
            ? DateTimeOffset.UtcNow.Add(expiry.Value)
            : DateTimeOffset.UtcNow.AddMinutes(5);

        _entries[key] = new CacheEntry(JsonSerializer.Serialize(value, JsonOptions), expiresAt);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        foreach (var key in _entries.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            _entries.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private sealed record CacheEntry(string Json, DateTimeOffset? ExpiresAtUtc);
}
