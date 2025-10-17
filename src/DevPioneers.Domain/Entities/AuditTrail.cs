// ============================================
// File: DevPioneers.Domain/Entities/AuditTrail.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Audit trail entity to track all system activities
/// </summary>
public class AuditTrail : BaseEntity
{
    /// <summary>
    /// User who performed the action (nullable for system actions)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Full name of the user who performed the action
    /// </summary>
    public string UserFullName { get; set; } = string.Empty;

    /// <summary>
    /// Entity name (table name)
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID (nullable for bulk operations)
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Old values (JSON) - for Update and Delete operations
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (JSON) - for Create and Update operations
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// IP Address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User Agent (browser/device info)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request method (GET, POST, PUT, DELETE)
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// Request URL/endpoint
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Exception details if operation failed
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation: User who performed the action
    /// </summary>
    public virtual User? User { get; set; }
}