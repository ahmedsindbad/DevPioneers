// ============================================
// File: DevPioneers.Domain/Enums/AuditAction.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Audit trail action types
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Create operation (INSERT)
    /// </summary>
    Create = 1,

    /// <summary>
    /// Read operation (SELECT) - Optional tracking
    /// </summary>
    Read = 2,

    /// <summary>
    /// Update operation (UPDATE)
    /// </summary>
    Update = 3,

    /// <summary>
    /// Delete operation (DELETE/Soft Delete)
    /// </summary>
    Delete = 4,

    /// <summary>
    /// Login action
    /// </summary>
    Login = 5,

    /// <summary>
    /// Logout action
    /// </summary>
    Logout = 6,

    /// <summary>
    /// Password change
    /// </summary>
    PasswordChange = 7,

    /// <summary>
    /// Role assignment
    /// </summary>
    RoleAssign = 8,

    /// <summary>
    /// Payment transaction
    /// </summary>
    Payment = 9,

    /// <summary>
    /// Subscription change
    /// </summary>
    Subscription = 10
}