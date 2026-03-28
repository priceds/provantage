using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProVantage.Domain.Interfaces;
using ProVantage.Infrastructure.Persistence;

namespace ProVantage.API.Middleware;

/// <summary>
/// Resolves the tenant from the JWT 'tenant_id' claim and sets it
/// on the scoped ICurrentTenantService for the request lifetime.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService, ApplicationDbContext db)
    {
        var tenantClaim = context.User.FindFirstValue("tenant_id");

        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

            if (tenant is not null)
            {
                tenantService.SetTenant(tenant.Id, tenant.Name);
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Adds a correlation ID to every request for distributed tracing.
/// </summary>
public class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public RequestCorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
