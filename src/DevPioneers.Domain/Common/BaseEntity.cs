// ============================================
// File: DevPioneers.Domain/Common/BaseEntity.cs
// ============================================
namespace DevPioneers.Domain.Common;

/// <summary>
/// Base entity with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date and time when entity was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Date and time when entity was deleted (UTC)
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
