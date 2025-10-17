// DevPioneers.Domain/Entities/OtpCode.cs
using System.ComponentModel.DataAnnotations.Schema;
using DevPioneers.Domain.Common;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// OTP (One-Time Password) code entity for mobile verification
/// </summary>
public class OtpCode : BaseEntity
{
    /// <summary>
    /// User ID (nullable for registration flow)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Navigation: related user (optional)
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Mobile number
    /// </summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// Email address (alternative to mobile)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// OTP code (hashed)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// OTP purpose (Login, Registration, PasswordReset, etc.)
    /// </summary>
    public string Purpose { get; set; } = "Login";

    /// <summary>
    /// Expiry date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Verified at
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Number of verification attempts
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Max allowed attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Check if OTP is valid
    /// </summary>
    public bool IsValid => !IsExpired && !IsVerified && !IsMaxAttemptsReached;

    /// <summary>
    /// Check if OTP is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if OTP is verified
    /// </summary>
    [NotMapped]
    public bool IsVerified => DateTime.UtcNow >= VerifiedAt;

    /// <summary>
    /// Check if max attempts reached
    /// </summary>
    public bool IsMaxAttemptsReached => Attempts >= MaxAttempts;

    /// <summary>
    /// Increment attempt count
    /// </summary>
    public void IncrementAttempts()
    {
        Attempts++;
    }

    /// <summary>
    /// Mark as verified
    /// </summary>
    public void MarkAsVerified()
    {
        VerifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// OTP purposes
    /// </summary>
    public static class Purposes
    {
        public const string Login = "Login";
        public const string Registration = "Registration";
        public const string PasswordReset = "PasswordReset";
        public const string PhoneVerification = "PhoneVerification";
        public const string EmailVerification = "EmailVerification";
    }
}
