// ============================================
// File: DevPioneers.Persistence/Interceptors/AuditInterceptor.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace DevPioneers.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges Interceptor for automatic audit trail logging
/// Captures all CRUD operations and stores old/new values as JSON
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public AuditInterceptor(
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    /// <summary>
    /// Before SaveChanges - Capture audit trail entries
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await AuditEntitiesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Synchronous SaveChanges override
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AuditEntities(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Main audit logic - captures all changed entities (Async)
    /// </summary>
    private async Task AuditEntitiesAsync(DbContext context, CancellationToken cancellationToken)
    {
        AuditEntities(context);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Main audit logic - captures all changed entities (Sync)
    /// </summary>
    private void AuditEntities(DbContext context)
    {
        // Get current user info
        var userId = _currentUserService.UserId;
        var userFullName = _currentUserService.UserFullName ?? "System";
        var ipAddress = _currentUserService.IpAddress ?? "Unknown";
        var userAgent = _currentUserService.UserAgent ?? "Unknown";

        // Get all tracked entities that are Added, Modified, or Deleted
        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity &&
                       (e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted))
            .ToList();

        var auditTrails = new List<AuditTrail>();

        foreach (var entry in entries)
        {
            var auditTrail = CreateAuditTrail(entry, userId, userFullName, ipAddress, userAgent);
            if (auditTrail != null)
            {
                auditTrails.Add(auditTrail);
            }
        }

        // Add audit trails to context
        if (auditTrails.Any())
        {
            context.Set<AuditTrail>().AddRange(auditTrails);
        }
    }

    /// <summary>
    /// Create audit trail entry for a specific entity change
    /// </summary>
    private AuditTrail? CreateAuditTrail(
        EntityEntry entry,
        int? userId,
        string userFullName,
        string ipAddress,
        string userAgent)
    {
        var entity = entry.Entity as BaseEntity;
        if (entity == null) return null;

        var entityType = entry.Entity.GetType();
        var entityName = entityType.Name;

        // Determine action
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => AuditAction.Read
        };

        // Capture old and new values
        var oldValues = GetOldValues(entry);
        var newValues = GetNewValues(entry);

        // Skip if no meaningful changes
        if (action == AuditAction.Update && string.IsNullOrEmpty(oldValues) && string.IsNullOrEmpty(newValues))
        {
            return null;
        }

        return new AuditTrail
        {
            UserId = userId,
            UserFullName = userFullName,
            EntityName = entityName,
            EntityId = GetEntityId(entity),
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = _dateTime.UtcNow,
            Metadata = GetMetadata(entry)
        };
    }

    /// <summary>
    /// Get entity ID (handles different primary key types)
    /// </summary>
    private int? GetEntityId(BaseEntity entity)
    {
        try
        {
            return entity.Id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get old values as JSON (for Update and Delete operations)
    /// </summary>
    private string? GetOldValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return null;

        var oldValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.OriginalValue != null)
            {
                oldValues[property.Metadata.Name] = property.OriginalValue;
            }
        }

        return oldValues.Any() ? JsonSerializer.Serialize(oldValues) : null;
    }

    /// <summary>
    /// Get new values as JSON (for Add and Update operations)
    /// </summary>
    private string? GetNewValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted)
            return null;

        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.CurrentValue != null)
            {
                newValues[property.Metadata.Name] = property.CurrentValue;
            }
        }

        return newValues.Any() ? JsonSerializer.Serialize(newValues) : null;
    }

    /// <summary>
    /// Get additional metadata about the change
    /// </summary>
    private string? GetMetadata(EntityEntry entry)
    {
        var metadata = new Dictionary<string, object>
        {
            ["EntityState"] = entry.State.ToString(),
            ["Timestamp"] = _dateTime.UtcNow,
            ["AssemblyName"] = entry.Entity.GetType().Assembly.GetName().Name ?? "Unknown"
        };

        // Add changed properties for Update operations
        if (entry.State == EntityState.Modified)
        {
            var changedProperties = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            if (changedProperties.Any())
            {
                metadata["ChangedProperties"] = changedProperties;
            }
        }

        return JsonSerializer.Serialize(metadata);
    }
}