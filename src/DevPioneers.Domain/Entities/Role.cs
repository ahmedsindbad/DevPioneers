// ============================================
// File: DevPioneers.Domain/Entities/Role.cs
// ============================================
using DevPioneers.Domain.Common;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Role entity for role-based access control
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// Role name (unique)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized role name for lookups
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if this is a system role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Navigation: Users in this role
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Common role names
    /// </summary>
    public static class Names
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string User = "User";
    }
}
