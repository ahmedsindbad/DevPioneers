// ============================================
// File: DevPioneers.Domain/Entities/User.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;
using BCrypt.Net;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// User entity representing system users
/// </summary>
public class User : AuditableEntity
{
    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email address (unique, used for login)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email verified flag
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Email verification date
    /// </summary>
    public DateTime? EmailVerifiedAt { get; set; }

    /// <summary>
    /// Mobile phone number (unique, used for login)
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// Mobile verified flag
    /// </summary>
    public bool MobileVerified { get; set; } = false;

    /// <summary>
    /// Mobile verification date
    /// </summary>
    public DateTime? MobileVerifiedAt { get; set; }

    /// <summary>
    /// Hashed password
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User account status
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Pending;

    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Two-factor authentication enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Two-factor authentication secret key
    /// </summary>
    public string? TwoFactorSecretKey { get; set; }

    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Last login IP address
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// Failed login attempts count
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Account locked until (after multiple failed attempts)
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Password reset token
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Password reset token expiry
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }

    /// <summary>
    /// Email verification token
    /// </summary>
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Navigation: User roles
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Navigation: User subscriptions
    /// </summary>
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();

    /// <summary>
    /// Navigation: User wallet
    /// </summary>
    public virtual Wallet? Wallet { get; set; }

    /// <summary>
    /// Navigation: User payments
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Navigation: Refresh tokens
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Check if user is active
    /// </summary>
    public bool IsActive() => Status == UserStatus.Active && !IsDeleted;

    /// <summary>
    /// Check if account is locked
    /// </summary>
    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    public bool HasRole(string roleName)
    {
        return UserRoles.Any(ur => ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Increment failed login attempts
    /// </summary>
    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            // Lock account for 30 minutes after 5 failed attempts
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
    }

    /// <summary>
    /// Reset failed login attempts
    /// </summary>
    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }
    /// <summary>
    /// Verify password against stored hash
    /// </summary>
    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }

    /// <summary>
    /// Record failed login attempt
    /// </summary>
    public void RecordFailedLogin()
    {
        IncrementFailedLoginAttempts();
    }

    /// <summary>
    /// Record successful login
    /// </summary>
    public void RecordSuccessfulLogin(string? ipAddress = null)
    {
        ResetFailedLoginAttempts();
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
    }

    /// <summary>
    /// Check if account is currently locked (alias for IsLocked)
    /// </summary>
    public bool IsAccountLocked() => IsLocked();
}