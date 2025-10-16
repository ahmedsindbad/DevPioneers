// ============================================
// File: DevPioneers.Domain/Entities/RefreshToken.cs
// ============================================
using DevPioneers.Domain.Common;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Refresh token entity for JWT token refresh mechanism
/// </summary>
public class RefreshToken : BaseEntity
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
    /// Token value (hashed)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiry date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Token creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when token was used (refreshed)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Date when token was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Revocation reason
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// IP address when token was created
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address when token was used
    /// </summary>
    public string? UsedByIp { get; set; }

    /// <summary>
    /// IP address when token was revoked
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// User agent when token was created
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device identifier (optional)
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Replaced by token (for rotation)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Check if token is active
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired && !IsUsed;

    /// <summary>
    /// Check if token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if token is revoked
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Check if token is used
    /// </summary>
    public bool IsUsed => UsedAt != null;

    /// <summary>
    /// Revoke token
    /// </summary>
    public void Revoke(string? ipAddress = null, string? reason = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        RevokedReason = reason ?? "Token manually revoked";
    }

    /// <summary>
    /// Mark token as used
    /// </summary>
    public void MarkAsUsed(string? ipAddress = null)
    {
        UsedAt = DateTime.UtcNow;
        UsedByIp = ipAddress;
    }

    /// <summary>
    /// Check if token can be refreshed
    /// </summary>
    public bool CanBeRefreshed()
    {
        return IsActive && !IsExpired;
    }
}