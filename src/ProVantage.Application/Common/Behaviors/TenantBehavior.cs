using MediatR;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Application.Common.Behaviors;

/// <summary>
/// Ensures the tenant context is resolved for every request.
/// This behavior sets up tenant isolation at the application level.
/// </summary>
public sealed class TenantBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantService _tenantService;

    public TenantBehavior(ICurrentTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_tenantService.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is not established.");
        }

        return await next();
    }
}
