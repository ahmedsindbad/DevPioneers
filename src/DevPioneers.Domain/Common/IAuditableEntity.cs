// ============================================
// File: DevPioneers.Domain/Common/IAuditableEntity.cs
// ============================================
namespace DevPioneers.Domain.Common;

/// <summary>
/// Interface for entities that require audit tracking
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// User ID who created the entity
    /// </summary>
    int? CreatedById { get; set; }

    /// <summary>
    /// User ID who last updated the entity
    /// </summary>
    int? UpdatedById { get; set; }

    /// <summary>
    /// User ID who deleted the entity
    /// </summary>
    int? DeletedById { get; set; }
}
