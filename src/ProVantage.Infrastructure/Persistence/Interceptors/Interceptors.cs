using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProVantage.Domain.Common;
using ProVantage.Domain.Interfaces;

namespace ProVantage.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF SaveChanges interceptor that automatically populates audit fields
/// (CreatedBy, ModifiedBy, timestamps, TenantId) on every save.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICurrentTenantService _currentTenant;

    public AuditableEntityInterceptor(
        ICurrentUserService currentUser,
        ICurrentTenantService currentTenant)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUser.Email;
                    entry.Entity.TenantId = _currentTenant.TenantId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = _currentUser.Email;
                    break;
            }
        }

        // Handle base entity timestamps for non-auditable entities
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity is not AuditableEntity))
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

/// <summary>
/// Interceptor that converts hard deletes into soft deletes automatically.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
