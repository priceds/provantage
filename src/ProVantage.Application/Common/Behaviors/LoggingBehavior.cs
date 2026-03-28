using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ProVantage.Application.Common.Behaviors;

/// <summary>
/// Logs request/response with elapsed time. Warns on slow queries (> 500ms).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling {RequestName} {@Request}", requestName, request);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Long-running request: {RequestName} ({ElapsedMs}ms) {@Request}",
                requestName, stopwatch.ElapsedMilliseconds, request);
        }
        else
        {
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
