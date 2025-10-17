// ============================================
// File: DevPioneers.Infrastructure/Services/CurrentUserService.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DevPioneers.Infrastructure.Services;

/// <summary>
/// Real implementation of CurrentUserService
/// Extracts user information from JWT claims in HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                           ?? _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? UserFullName =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("full_name")?.Value;

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? UserEmail => Email; // Alias for Email property

    public IEnumerable<string> Roles
    {
        get
        {
            var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
                       ?? _httpContextAccessor.HttpContext?.User?.FindAll("role")
                       ?? Enumerable.Empty<Claim>();

            return roles.Select(r => r.Value).Distinct();
        }
    }

    public IEnumerable<string> UserRoles => Roles; // Alias for Roles property

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // Check for X-Forwarded-For header (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to RemoteIpAddress
            return context.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].FirstOrDefault();

    public string? RequestPath =>
        _httpContextAccessor.HttpContext?.Request?.Path.Value;

    public string? HttpMethod =>
        _httpContextAccessor.HttpContext?.Request?.Method;

    public bool IsInRole(string role)
    {
        if (string.IsNullOrEmpty(role))
            return false;

        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}