using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ProVantage.API.Middleware;

/// <summary>
/// Global exception handler converting exceptions to RFC 7807 Problem Details.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred.",
                    Extensions =
                    {
                        ["errors"] = validationEx.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                    }
                }),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = exception.Message
                }),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = exception.Message
                }),

            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title = "Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = exception.Message
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred."
                })
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        var correlationId = context.Items["CorrelationId"]?.ToString();
        if (correlationId is not null)
            problemDetails.Extensions["correlationId"] = correlationId;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
