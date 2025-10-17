// ============================================
// File: DevPioneers.Application/Common/Interfaces/ICurrentUserService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current user context information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Current user ID (null if not authenticated)
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// Current user full name
    /// </summary>
    string? UserFullName { get; }

    /// <summary>
    /// Current user email
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Current user roles
    /// </summary>
    IEnumerable<string> UserRoles { get; }

    /// <summary>
    /// Check if user is in specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// IP address of current request
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// User agent of current request
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Request path
    /// </summary>
    string? RequestPath { get; }

    /// <summary>
    /// HTTP method
    /// </summary>
    string? HttpMethod { get; }
}