using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : Guid.Empty;

    public string Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string Role =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

public class CurrentTenantService : ICurrentTenantService
{
    public Guid TenantId { get; private set; }
    public string TenantName { get; private set; } = string.Empty;

    public void SetTenant(Guid tenantId, string tenantName)
    {
        TenantId = tenantId;
        TenantName = tenantName;
    }
}
