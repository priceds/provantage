using MediatR;
using Microsoft.Extensions.Logging;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results in Redis.
/// Only activates for requests implementing ICacheable.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheable
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;

        var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);

        var response = await next();
        await _cache.SetAsync(cacheKey, response, request.Expiration, cancellationToken);

        return response;
    }
}
