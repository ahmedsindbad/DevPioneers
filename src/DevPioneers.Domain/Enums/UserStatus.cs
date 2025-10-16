// ============================================
// File: DevPioneers.Domain/Enums/UserStatus.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// User account status
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Account pending email/phone verification
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Account active and verified
    /// </summary>
    Active = 1,

    /// <summary>
    /// Account temporarily suspended
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Account banned
    /// </summary>
    Banned = 3,

    /// <summary>
    /// Account deactivated by user
    /// </summary>
    Deactivated = 4
}
