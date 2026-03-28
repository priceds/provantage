using MediatR;

namespace ProVantage.Application.Common.Interfaces;

/// <summary>Marker for CQRS commands that return a result.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>Marker for CQRS commands that return nothing.</summary>
public interface ICommand : IRequest;

/// <summary>Marker for CQRS queries that return a result.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Marker interface for queries eligible for Redis caching via CachingBehavior.
/// </summary>
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
