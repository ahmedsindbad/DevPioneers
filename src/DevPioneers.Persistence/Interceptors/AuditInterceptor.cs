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
    /// Main audit logic - captures all changed entities
    /// </summary>
    private async Task AuditEntitiesAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Get current user info
        var userId = _currentUserService.UserId;
        var userFullName = _currentUserService.UserFullName;
        var ipAddress = _currentUserService.IpAddress;
        var userAgent = _currentUserService.UserAgent;

        // Get all tracked entities that are Added, Modified, or Deleted
        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity &&
                       (e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted))
            .ToList();

        var auditEntries = new List<AuditTrail>();

        foreach (var entry in entries)
        {
            // Skip AuditTrail entity itself to avoid infinite loop
            if (entry.Entity is AuditTrail)
                continue;

            var auditEntry = CreateAuditEntry(
                entry,
                userId,
                userFullName,
                ipAddress,
                userAgent);

            if (auditEntry != null)
            {
                auditEntries.Add(auditEntry);
            }

            // Update auditable entity fields
            UpdateAuditableEntity(entry, userId);
        }

        // Add audit entries to context
        if (auditEntries.Any())
        {
            await context.Set<AuditTrail>().AddRangeAsync(auditEntries, cancellationToken);
        }
    }

    /// <summary>
    /// Create audit trail entry from entity entry
    /// </summary>
    private AuditTrail? CreateAuditEntry(
        EntityEntry entry,
        int? userId,
        string? userFullName,
        string? ipAddress,
        string? userAgent)
    {
        var entity = (BaseEntity)entry.Entity;
        var entityName = entry.Entity.GetType().Name;

        // Determine action
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => (AuditAction?)null
        };

        if (action == null)
            return null;

        // Capture old and new values
        var oldValues = GetOldValues(entry);
        var newValues = GetNewValues(entry);

        return new AuditTrail
        {
            UserId = userId,
            UserFullName = userFullName,
            EntityName = entityName,
            EntityId = entity.Id,
            Action = action.Value,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TimestampUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get old values (before change) as JSON
    /// </summary>
    private string? GetOldValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return null;

        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            // Skip navigation properties and complex types
            if (property.Metadata.IsForeignKey() ||
                property.Metadata.IsKey())
                continue;

            var originalValue = property.OriginalValue;

            // Only include if value changed
            if (entry.State == EntityState.Modified)
            {
                if (Equals(originalValue, property.CurrentValue))
                    continue;
            }

            values[property.Metadata.Name] = originalValue;
        }

        return values.Any()
            ? JsonSerializer.Serialize(values, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            })
            : null;
    }

    /// <summary>
    /// Get new values (after change) as JSON
    /// </summary>
    private string? GetNewValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted)
            return null;

        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            // Skip navigation properties and complex types
            if (property.Metadata.IsForeignKey() ||
                property.Metadata.IsKey())
                continue;

            var currentValue = property.CurrentValue;

            // Only include if value changed
            if (entry.State == EntityState.Modified)
            {
                if (Equals(property.OriginalValue, currentValue))
                    continue;
            }

            values[property.Metadata.Name] = currentValue;
        }

        return values.Any()
            ? JsonSerializer.Serialize(values, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            })
            : null;
    }

    /// <summary>
    /// Update CreatedById, UpdatedById, DeletedById for auditable entities
    /// </summary>
    private void UpdateAuditableEntity(EntityEntry entry, int? userId)
    {
        if (entry.Entity is not IAuditableEntity auditableEntity)
            return;

        switch (entry.State)
        {
            case EntityState.Added:
                auditableEntity.CreatedById = userId;
                break;

            case EntityState.Modified:
                auditableEntity.UpdatedById = userId;
                break;

            case EntityState.Deleted:
                // Soft delete - update DeletedById
                if (entry.Entity is BaseEntity baseEntity && baseEntity.IsDeleted)
                {
                    auditableEntity.DeletedById = userId;
                }
                break;
        }
    }
}
