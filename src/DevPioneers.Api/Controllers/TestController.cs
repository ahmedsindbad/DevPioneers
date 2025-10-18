// ============================================
// File: DevPioneers.Api/Controllers/TestController.cs
// Test Controller for JWT Authentication verification
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevPioneers.Api.Controllers;

/// <summary>
/// Test Controller for JWT Authentication verification
/// This controller provides endpoints to test JWT authentication and authorization
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class TestController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        ICurrentUserService currentUserService,
        ILogger<TestController> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Public endpoint - No authentication required
    /// </summary>
    /// <returns>Public information</returns>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult GetPublicData()
    {
        var response = ApiResponse.Ok(new
        {
            message = "This is a public endpoint",
            timestamp = DateTime.UtcNow,
            server = Environment.MachineName,
            version = "1.0.0",
            authentication = "Not required"
        });

        _logger.LogInformation("Public endpoint accessed from IP: {IpAddress}", 
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(response);
    }

    /// <summary>
    /// Protected endpoint - Requires valid JWT token
    /// </summary>
    /// <returns>User information from JWT token</returns>
    [HttpGet("protected")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetProtectedData()
    {
        var userInfo = new
        {
            userId = _currentUserService.UserId,
            email = _currentUserService.Email,
            fullName = _currentUserService.UserFullName,
            roles = _currentUserService.Roles.ToList(),
            isAuthenticated = _currentUserService.IsAuthenticated,
            ipAddress = _currentUserService.IpAddress,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            timestamp = DateTime.UtcNow,
            message = "JWT Authentication successful!"
        };

        var response = ApiResponse.Ok(userInfo);

        _logger.LogInformation("Protected endpoint accessed by user {UserId} ({Email})", 
            _currentUserService.UserId, _currentUserService.Email);

        return Ok(response);
    }

    /// <summary>
    /// Admin only endpoint - Requires Admin role
    /// </summary>
    /// <returns>Admin-specific information</returns>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminData()
    {
        var adminInfo = new
        {
            message = "Welcome Admin!",
            userId = _currentUserService.UserId,
            email = _currentUserService.Email,
            roles = _currentUserService.Roles.ToList(),
            systemInfo = new
            {
                serverTime = DateTime.UtcNow,
                serverName = Environment.MachineName,
                processId = Environment.ProcessId,
                memoryUsage = GC.GetTotalMemory(false),
                dotNetVersion = Environment.Version.ToString()
            },
            permissions = new[]
            {
                "CanViewAllUsers",
                "CanManageSubscriptions", 
                "CanAccessReports",
                "CanManagePayments",
                "CanViewAuditTrail"
            }
        };

        var response = ApiResponse.Ok(adminInfo);

        _logger.LogInformation("Admin endpoint accessed by user {UserId} ({Email})", 
            _currentUserService.UserId, _currentUserService.Email);

        return Ok(response);
    }

    /// <summary>
    /// Manager or Admin endpoint - Requires Manager or Admin role
    /// </summary>
    /// <returns>Manager-level information</returns>
    [HttpGet("manager-or-admin")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public IActionResult GetManagerData()
    {
        var managerInfo = new
        {
            message = "Welcome Manager/Admin!",
            userId = _currentUserService.UserId,
            email = _currentUserService.Email,
            roles = _currentUserService.Roles.ToList(),
            dashboardStats = new
            {
                totalUsers = 150, // Mock data
                activeSubscriptions = 75,
                totalRevenue = 25000.50m,
                pendingPayments = 5
            },
            permissions = new[]
            {
                "CanViewReports",
                "CanManageUsers",
                "CanViewSubscriptions"
            }
        };

        var response = ApiResponse.Ok(managerInfo);

        _logger.LogInformation("Manager endpoint accessed by user {UserId} ({Email})", 
            _currentUserService.UserId, _currentUserService.Email);

        return Ok(response);
    }

    /// <summary>
    /// Test token validation endpoint
    /// </summary>
    /// <returns>Token validation details</returns>
    [HttpGet("validate-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        var tokenInfo = new
        {
            isValid = true,
            tokenType = "JWT",
            userId = _currentUserService.UserId,
            email = _currentUserService.Email,
            roles = _currentUserService.Roles.ToList(),
            claims = User.Claims.Select(c => new 
            { 
                type = c.Type, 
                value = c.Value,
                shortType = c.Type.Split('/').LastOrDefault() ?? c.Type
            }).ToList(),
            tokenDetails = new
            {
                issuer = User.FindFirst("iss")?.Value,
                audience = User.FindFirst("aud")?.Value,
                issuedAt = User.FindFirst("iat")?.Value,
                jwtId = User.FindFirst("jti")?.Value,
                expirationTime = User.FindFirst("exp")?.Value
            },
            serverTime = DateTime.UtcNow,
            timeZone = TimeZoneInfo.Local.Id
        };

        var response = ApiResponse.Ok(tokenInfo);

        return Ok(response);
    }

    /// <summary>
    /// Test different HTTP methods with authentication
    /// </summary>
    /// <returns>HTTP method test result</returns>
    [HttpPost("test-post")]
    [HttpPut("test-put")]
    [HttpDelete("test-delete")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult TestHttpMethods()
    {
        var methodInfo = new
        {
            method = HttpContext.Request.Method,
            path = HttpContext.Request.Path.Value,
            userId = _currentUserService.UserId,
            timestamp = DateTime.UtcNow,
            message = $"HTTP {HttpContext.Request.Method} method test successful with JWT authentication"
        };

        var response = ApiResponse.Ok(methodInfo);

        _logger.LogInformation("HTTP {Method} test endpoint accessed by user {UserId}", 
            HttpContext.Request.Method, _currentUserService.UserId);

        return Ok(response);
    }

    /// <summary>
    /// Test endpoint for checking user permissions
    /// </summary>
    /// <returns>User permissions check</returns>
    [HttpGet("check-permissions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult CheckPermissions()
    {
        var permissions = new
        {
            userId = _currentUserService.UserId,
            email = _currentUserService.Email,
            roles = _currentUserService.Roles.ToList(),
            permissions = new
            {
                isAdmin = _currentUserService.IsInRole("Admin"),
                isManager = _currentUserService.IsInRole("Manager"),
                isUser = _currentUserService.IsInRole("User"),
                canAccessWallet = User.Identity?.IsAuthenticated == true,
                canManageSubscriptions = _currentUserService.IsInRole("Admin") || _currentUserService.IsInRole("Manager"),
                canViewReports = _currentUserService.IsInRole("Admin") || _currentUserService.IsInRole("Manager"),
                canAccessHangfireDashboard = _currentUserService.IsInRole("Admin")
            },
            authenticationMethod = "JWT",
            isAuthenticated = _currentUserService.IsAuthenticated,
            timestamp = DateTime.UtcNow
        };

        var response = ApiResponse.Ok(permissions);

        return Ok(response);
    }

    /// <summary>
    /// Test endpoint for debugging JWT claims
    /// </summary>
    /// <returns>All JWT claims for debugging</returns>
    [HttpGet("debug-claims")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult DebugClaims()
    {
        var debugInfo = new
        {
            httpContext = new
            {
                isAuthenticated = HttpContext.User?.Identity?.IsAuthenticated,
                authenticationType = HttpContext.User?.Identity?.AuthenticationType,
                name = HttpContext.User?.Identity?.Name
            },
            currentUserService = new
            {
                userId = _currentUserService.UserId,
                email = _currentUserService.Email,
                fullName = _currentUserService.UserFullName,
                roles = _currentUserService.Roles.ToList(),
                isAuthenticated = _currentUserService.IsAuthenticated,
                ipAddress = _currentUserService.IpAddress
            },
            allClaims = User.Claims?.Select(c => new
            {
                type = c.Type,
                value = c.Value,
                valueType = c.ValueType,
                originalIssuer = c.OriginalIssuer,
                issuer = c.Issuer
            }).ToList(),
            headers = HttpContext.Request.Headers
                .Where(h => h.Key.StartsWith("Authorization") || h.Key.StartsWith("X-"))
                .ToDictionary(h => h.Key, h => h.Value.ToString()),
            timestamp = DateTime.UtcNow
        };

        var response = ApiResponse.Ok(debugInfo);

        return Ok(response);
    }
}