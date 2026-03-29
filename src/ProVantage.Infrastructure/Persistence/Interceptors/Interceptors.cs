using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProVantage.Domain.Common;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using System.Text.Json;

namespace ProVantage.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF SaveChanges interceptor that automatically populates audit fields
/// (CreatedBy, ModifiedBy, timestamps, TenantId) on every save.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly HashSet<string> IgnoredAuditProperties =
    [
        nameof(BaseEntity.CreatedAt),
        nameof(BaseEntity.UpdatedAt),
        nameof(BaseEntity.IsDeleted),
        nameof(BaseEntity.DeletedAt),
        nameof(AuditableEntity.CreatedBy),
        nameof(AuditableEntity.ModifiedBy)
    ];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuditableEntityInterceptor(
        ICurrentUserService currentUser,
        ICurrentTenantService currentTenant,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditLogs = CaptureAuditLogs(context.ChangeTracker);

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                    }

                    if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy) &&
                        !string.IsNullOrWhiteSpace(_currentUser.Email))
                    {
                        entry.Entity.CreatedBy = _currentUser.Email;
                    }

                    if (entry.Entity.TenantId == Guid.Empty && _currentTenant.TenantId != Guid.Empty)
                    {
                        entry.Entity.TenantId = _currentTenant.TenantId;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(_currentUser.Email))
                    {
                        entry.Entity.ModifiedBy = _currentUser.Email;
                    }
                    break;
            }
        }

        // Handle base entity timestamps for non-auditable entities
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity is not AuditableEntity))
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditLog> CaptureAuditLogs(ChangeTracker changeTracker)
    {
        if (!_currentUser.IsAuthenticated || _currentTenant.TenantId == Guid.Empty)
        {
            return [];
        }

        var logs = new List<AuditLog>();

        foreach (var entry in changeTracker.Entries()
                     .Where(e =>
                         e.Entity is not AuditLog &&
                         e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            var oldValues = entry.State is EntityState.Modified or EntityState.Deleted
                ? SerializeValues(entry, useOriginalValues: true, onlyModified: entry.State == EntityState.Modified)
                : null;

            var newValues = entry.State is EntityState.Added or EntityState.Modified
                ? SerializeValues(entry, useOriginalValues: false, onlyModified: entry.State == EntityState.Modified)
                : null;

            if (action == AuditAction.Updated && string.IsNullOrWhiteSpace(oldValues) && string.IsNullOrWhiteSpace(newValues))
            {
                continue;
            }

            logs.Add(new AuditLog
            {
                TenantId = ResolveTenantId(entry),
                UserId = _currentUser.UserId,
                UserName = !string.IsNullOrWhiteSpace(_currentUser.Email)
                    ? _currentUser.Email
                    : _currentUser.UserId.ToString(),
                Action = action,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = ResolveEntityId(entry),
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()
            });
        }

        return logs.Where(log => log.TenantId != Guid.Empty && log.EntityId != Guid.Empty).ToList();
    }

    private Guid ResolveTenantId(EntityEntry entry)
    {
        if (entry.Entity is AuditableEntity auditable && auditable.TenantId != Guid.Empty)
        {
            return auditable.TenantId;
        }

        return _currentTenant.TenantId;
    }

    private static Guid ResolveEntityId(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(BaseEntity.Id));
        var currentValue = idProperty?.CurrentValue ?? idProperty?.OriginalValue;
        return currentValue is Guid id ? id : Guid.Empty;
    }

    private static string? SerializeValues(EntityEntry entry, bool useOriginalValues, bool onlyModified)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (IgnoredAuditProperties.Contains(property.Metadata.Name))
            {
                continue;
            }

            if (onlyModified && !property.IsModified)
            {
                continue;
            }

            var value = useOriginalValues ? property.OriginalValue : property.CurrentValue;
            values[property.Metadata.Name] = value;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
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
