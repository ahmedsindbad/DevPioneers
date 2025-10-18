// ============================================
// File: DevPioneers.Api/Controllers/AuthController.cs
// Authentication Controller - Handles all authentication related operations
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.Commands;
using DevPioneers.Application.Features.Auth.DTOs;
using DevPioneers.Infrastructure.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DevPioneers.Infrastructure.Configurations;

namespace DevPioneers.Api.Controllers;

/// <summary>
/// Authentication Controller
/// Handles login, registration, logout, token refresh, OTP verification, and 2FA operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// User login with email/mobile and password
    /// Supports 2FA if enabled for the user
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT tokens</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new LoginCommand(
                EmailOrMobile: request.EmailOrMobile,
                Password: request.Password,
                RememberMe: request.RememberMe,
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgent()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for {EmailOrMobile}: {Error}", 
                    request.EmailOrMobile, result.Errors);
                return Unauthorized(result.Errors);
            }

            var authResponse = result.Data!;

            // If 2FA is required, return response without tokens
            if (authResponse.RequiresTwoFactor)
            {
                return Ok(new
                {
                    requiresTwoFactor = true,
                    twoFactorUserId = authResponse.TwoFactorUserId,
                    message = "Two-factor authentication code required"
                });
            }

            // Generate JWT tokens for successful login
            await GenerateAndSetTokensAsync(authResponse);

            _logger.LogInformation("User {UserId} logged in successfully", authResponse.UserId);

            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {EmailOrMobile}", request.EmailOrMobile);
            return StatusCode(500, "An error occurred during login");
        }
    }

    /// <summary>
    /// Refresh JWT access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New JWT tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var refreshToken = request.RefreshToken ?? GetRefreshTokenFromCookies();
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized("Refresh token is required");
            }

            var command = new RefreshTokenCommand(
                RefreshToken: refreshToken,
                IpAddress: GetClientIpAddress()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Token refresh failed: {Error}", result.Errors);
                RemoveRefreshTokenCookie();
                return Unauthorized(result.Errors);
            }

            var authResponse = result.Data!;

            // Generate new JWT tokens
            await GenerateAndSetTokensAsync(authResponse);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", authResponse.UserId);

            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, "An error occurred during token refresh");
        }
    }

    /// <summary>
    /// Verify OTP code for 2FA login
    /// </summary>
    /// <param name="request">OTP verification request</param>
    /// <returns>Authentication response with JWT tokens</returns>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new VerifyOtpCommand(
                UserId: request.UserId,
                OtpCode: request.OtpCode,
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgent()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("OTP verification failed for user {UserId}: {Error}", 
                    request.UserId, result.Errors);
                return Unauthorized(result.Errors);
            }

            var authResponse = result.Data!;

            // Generate JWT tokens for successful 2FA
            await GenerateAndSetTokensAsync(authResponse);

            _logger.LogInformation("2FA verification successful for user {UserId}", authResponse.UserId);

            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP verification error for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred during OTP verification");
        }
    }

    /// <summary>
    /// Send OTP code via email or SMS
    /// </summary>
    /// <param name="request">OTP send request</param>
    /// <returns>Masked contact information</returns>
    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new SendOtpCommand(
                EmailOrMobile: request.EmailOrMobile,
                Purpose: Enum.Parse<OtpPurpose>(request.Purpose)
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("OTP send failed for {EmailOrMobile}: {Error}", 
                    request.EmailOrMobile, result.Errors);
                return BadRequest(result.Errors);
            }

            return Ok(new
            {
                message = "OTP sent successfully",
                sentTo = result.Data,
                expiresIn = 300 // 5 minutes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP send error for {EmailOrMobile}", request.EmailOrMobile);
            return StatusCode(500, "An error occurred while sending OTP");
        }
    }

    /// <summary>
    /// Logout current user and revoke refresh token
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var refreshToken = GetRefreshTokenFromCookies();
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _refreshTokenService.RevokeRefreshTokenAsync(
                    refreshToken, 
                    GetClientIpAddress(), 
                    "User logout"
                );
            }

            RemoveRefreshTokenCookie();

            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} logged out successfully", userId);

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, "An error occurred during logout");
        }
    }

    /// <summary>
    /// Logout from all devices (revoke all refresh tokens)
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            await _refreshTokenService.RevokeAllUserRefreshTokensAsync(
                userId, 
                GetClientIpAddress(), 
                "User logout from all devices"
            );

            RemoveRefreshTokenCookie();

            _logger.LogInformation("User {UserId} logged out from all devices", userId);

            return Ok(new { message = "Logged out from all devices successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout all error");
            return StatusCode(500, "An error occurred during logout");
        }
    }

    /// <summary>
    /// Get current user profile information
    /// </summary>
    /// <returns>User profile data</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // You can create GetUserProfileQuery if needed
            // For now, return basic info from JWT claims
            var userProfile = new UserProfileDto
            {
                UserId = userId,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Mobile = User.FindFirstValue("mobile") ?? string.Empty,
                EmailVerified = bool.Parse(User.FindFirstValue("emailVerified") ?? "false"),
                MobileVerified = bool.Parse(User.FindFirstValue("mobileVerified") ?? "false"),
                TwoFactorEnabled = bool.Parse(User.FindFirstValue("twoFactorEnabled") ?? "false"),
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            };

            return Ok(userProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get profile error");
            return StatusCode(500, "An error occurred while retrieving profile");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Generate JWT tokens and set them in response
    /// </summary>
    private async Task GenerateAndSetTokensAsync(AuthResponseDto authResponse)
    {
        // Generate access token
        var user = new Domain.Entities.User 
        { 
            Id = authResponse.UserId, 
            Email = authResponse.Email,
            FullName = authResponse.FullName
        };
        
        authResponse.AccessToken = _jwtTokenService.GenerateAccessToken(user, authResponse.Roles);
        authResponse.AccessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Generate refresh token
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
            authResponse.UserId,
            GetUserAgent(),
            GetClientIpAddress()
        );

        authResponse.RefreshToken = refreshToken.Token;
        authResponse.RefreshTokenExpires = refreshToken.ExpiresAt;

        // Set refresh token in HttpOnly cookie
        SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);
    }

    /// <summary>
    /// Set refresh token in HttpOnly cookie
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            Path = "/",
            IsEssential = true
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Get refresh token from cookie
    /// </summary>
    private string? GetRefreshTokenFromCookies()
    {
        return Request.Cookies["refreshToken"];
    }

    /// <summary>
    /// Remove refresh token cookie
    /// </summary>
    private void RemoveRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    /// <summary>
    /// Get client IP address
    /// </summary>
    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Get User-Agent header
    /// </summary>
    private string GetUserAgent()
    {
        return Request.Headers.UserAgent.ToString();
    }

    /// <summary>
    /// Get current user ID from JWT claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #endregion
}

#region DTOs for API Requests

/// <summary>
/// Refresh token request DTO
/// </summary>
public class RefreshTokenDto
{
    public string? RefreshToken { get; set; }
}

/// <summary>
/// OTP verification request DTO
/// </summary>
public class VerifyOtpDto
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must contain only numbers")]
    public string OtpCode { get; set; } = string.Empty;
}

/// <summary>
/// Send OTP request DTO
/// </summary>
public class SendOtpDto
{
    [Required(ErrorMessage = "Email or mobile is required")]
    public string EmailOrMobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Purpose is required")]
    public string Purpose { get; set; } = "TwoFactorAuth";
}

/// <summary>
/// User profile response DTO
/// </summary>
public class UserProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool MobileVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public List<string> Roles { get; set; } = new();
}

#endregion