// ============================================
// File: DevPioneers.Domain/Enums/AuditAction.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Audit trail action type
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Entity created
    /// </summary>
    Create = 1,

    /// <summary>
    /// Entity updated
    /// </summary>
    Update = 2,

    /// <summary>
    /// Entity deleted
    /// </summary>
    Delete = 3,

    /// <summary>
    /// User login
    /// </summary>
    Login = 4,

    /// <summary>
    /// User logout
    /// </summary>
    Logout = 5,

    /// <summary>
    /// Failed login attempt
    /// </summary>
    LoginFailed = 6,

    /// <summary>
    /// Password changed
    /// </summary>
    PasswordChanged = 7,

    /// <summary>
    /// Password reset requested
    /// </summary>
    PasswordResetRequested = 8,

    /// <summary>
    /// Email verified
    /// </summary>
    EmailVerified = 9,

    /// <summary>
    /// Phone verified
    /// </summary>
    PhoneVerified = 10,

    /// <summary>
    /// Two-factor authentication enabled
    /// </summary>
    TwoFactorEnabled = 11,

    /// <summary>
    /// Two-factor authentication disabled
    /// </summary>
    TwoFactorDisabled = 12
}