// ============================================
// File: DevPioneers.Domain/Common/AuditableEntity.cs
// ============================================
namespace DevPioneers.Domain.Common;

/// <summary>
/// Base entity with audit tracking
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public int? CreatedById { get; set; }
    public int? UpdatedById { get; set; }
    public int? DeletedById { get; set; }
}