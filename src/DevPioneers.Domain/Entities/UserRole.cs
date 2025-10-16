// ============================================
// File: DevPioneers.Domain/Entities/UserRole.cs
// ============================================
using DevPioneers.Domain.Common;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Many-to-many relationship between Users and Roles
/// </summary>
public class UserRole : BaseEntity
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigation: User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Role ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Navigation: Role
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Date when role was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who assigned this role
    /// </summary>
    public int? AssignedById { get; set; }
}
