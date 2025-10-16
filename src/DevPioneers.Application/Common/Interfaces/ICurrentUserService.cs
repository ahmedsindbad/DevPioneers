// ============================================
// File: DevPioneers.Application/Common/Interfaces/ICurrentUserService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Service to get current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Current user ID (from JWT claims)
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// Current user full name
    /// </summary>
    string? UserFullName { get; }

    /// <summary>
    /// Current user email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Current user roles
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Is user authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// IP Address of current request
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// User Agent of current request
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    bool IsInRole(string role);
}