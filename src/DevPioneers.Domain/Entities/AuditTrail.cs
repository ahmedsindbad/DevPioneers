// ============================================
// File: DevPioneers.Domain/Entities/AuditTrail.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Audit trail entity for tracking all system activities
/// </summary>
public class AuditTrail : BaseEntity
{
    /// <summary>
    /// User ID who performed the action (null for system actions)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// User full name (denormalized for performance)
    /// </summary>
    public string? UserFullName { get; set; }

    /// <summary>
    /// Entity name (table name)
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID (record ID)
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Old values (JSON) - before the change
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (JSON) - after the change
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent (browser/device info)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp (UTC)
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Request path (API endpoint)
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Request method (GET, POST, PUT, DELETE)
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// Response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Exception details (if any error occurred)
    /// </summary>
    public string? ExceptionDetails { get; set; }
}