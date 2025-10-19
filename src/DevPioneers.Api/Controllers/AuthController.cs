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
using DevPioneers.Domain.Enums;

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
    /// User registration (signup) with email/mobile verification
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication response for newly registered user</returns>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup([FromBody] RegisterDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new RegisterCommand(
                FullName: request.FullName,
                Email: request.Email,
                Mobile: request.Mobile,
                Password: request.Password,
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgent()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Registration failed for {Email}: {Error}",
                    request.Email, result.Errors);

                // Check if it's a duplicate error (409 Conflict)
                if (result.Errors.Any(e => e.Contains("already registered") || e.Contains("already exists")))
                {
                    return Conflict(result.Errors);
                }

                return BadRequest(result.Errors);
            }

            var authResponse = result.Data!;

            // Send OTP for email/mobile verification if required
            if (!string.IsNullOrEmpty(request.Mobile))
            {
                try
                {
                    var sendOtpCommand = new SendOtpCommand(
                        EmailOrMobile: request.Mobile,
                        Purpose: OtpPurpose.Registration
                    );

                    await _mediator.Send(sendOtpCommand);

                    _logger.LogInformation("Mobile verification OTP sent to new user {UserId}", authResponse.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send mobile OTP for new user {UserId}", authResponse.UserId);
                    // Don't fail registration if OTP sending fails
                }
            }

            // For new registrations, typically we don't auto-login
            // User needs to verify email/mobile first
            authResponse.RequiresEmailVerification = true;

            // Only generate tokens if email verification is not required
            // In most cases, you'd want users to verify email first
            var requireEmailVerification = true; // Set based on your business logic

            if (!requireEmailVerification)
            {
                await GenerateAndSetTokensAsync(authResponse);
            }

            _logger.LogInformation("User {UserId} registered successfully with email {Email}",
                authResponse.UserId, authResponse.Email);

            // Return 201 Created for successful registration
            return Created($"/api/auth/profile", new
            {
                message = "Registration successful! Please check your email for verification instructions.",
                user = authResponse,
                nextSteps = new[]
                {
                requireEmailVerification ? "Verify your email address" : null,
                !string.IsNullOrEmpty(request.Mobile) ? "Verify your mobile number using the OTP sent" : null
            }.Where(step => !string.IsNullOrEmpty(step)).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for {Email}", request.Email);
            return StatusCode(500, "An error occurred during registration");
        }
    }

    /// <summary>
    /// Verify email address using verification token
    /// </summary>
    /// <param name="request">Email verification request</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new VerifyEmailCommand(
                Token: request.Token,
                IpAddress: GetClientIpAddress()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Email verification failed for token {Token}: {Error}",
                    request.Token, result.Errors);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Email verified successfully for user");

            return Ok(new
            {
                message = "Email verified successfully! You can now login to your account.",
                verified = true,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification error for token {Token}", request.Token);
            return StatusCode(500, "An error occurred during email verification");
        }
    }

    /// <summary>
    /// Resend email verification link
    /// </summary>
    /// <param name="request">Resend verification request</param>
    /// <returns>Success message</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendEmailVerification([FromBody] ResendVerificationDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new ResendEmailVerificationCommand(
                Email: request.Email,
                IpAddress: GetClientIpAddress()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Resend verification failed for {Email}: {Error}",
                    request.Email, result.Errors);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Verification email resent to {Email}", request.Email);

            return Ok(new
            {
                message = "Verification email sent successfully! Please check your inbox.",
                email = MaskEmail(request.Email),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification error for {Email}", request.Email);
            return StatusCode(500, "An error occurred while sending verification email");
        }
    }

    /// <summary>
    /// Helper method to mask email for security
    /// </summary>
    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;

        var username = parts[0];
        var domain = parts[1];

        if (username.Length <= 2)
            return $"{username[0]}***@{domain}";

        return $"{username[0]}***{username[^1]}@{domain}";
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

    /// <summary>
    /// Verify mobile number using OTP code
    /// </summary>
    /// <param name="request">Mobile verification request</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify-mobile")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyMobile([FromBody] VerifyMobileDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new VerifyMobileCommand(
                Mobile: request.Mobile,
                OtpCode: request.OtpCode,
                IpAddress: GetClientIpAddress()
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Mobile verification failed for {Mobile}: {Error}",
                    request.Mobile, result.Errors);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Mobile verified successfully for {Mobile}", request.Mobile);

            return Ok(new
            {
                message = "Mobile number verified successfully!",
                mobile = MaskMobile(request.Mobile),
                verified = true,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile verification error for {Mobile}", request.Mobile);
            return StatusCode(500, "An error occurred during mobile verification");
        }
    }

    /// <summary>
    /// Send OTP to mobile number for verification
    /// </summary>
    /// <param name="request">Mobile OTP request</param>
    /// <returns>Success message with masked mobile</returns>
    [HttpPost("send-mobile-otp")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMobileOtp([FromBody] SendMobileOtpDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new SendOtpCommand(
                EmailOrMobile: request.Mobile,
                Purpose: Enum.Parse<OtpPurpose>(request.Purpose)
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Mobile OTP send failed for {Mobile}: {Error}",
                    request.Mobile, result.Errors);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Mobile OTP sent successfully to {Mobile}", request.Mobile);

            return Ok(new
            {
                message = "OTP sent successfully to your mobile number!",
                mobile = MaskMobile(request.Mobile),
                expiresInMinutes = 10,
                purpose = request.Purpose,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send mobile OTP error for {Mobile}", request.Mobile);
            return StatusCode(500, "An error occurred while sending OTP");
        }
    }

    /// <summary>
    /// Check if email or mobile is already registered
    /// </summary>
    /// <param name="emailOrMobile">Email or mobile to check</param>
    /// <returns>Availability status</returns>
    [HttpGet("check-availability")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckAvailability([FromQuery] string emailOrMobile)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(emailOrMobile))
            {
                return BadRequest("Email or mobile parameter is required");
            }

            var command = new CheckUserExistsCommand(EmailOrMobile: emailOrMobile);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Check availability failed for {EmailOrMobile}: {Error}",
                    emailOrMobile, result.Errors);
                return BadRequest(result.Errors);
            }

            var exists = result.Data;
            var isEmail = emailOrMobile.Contains('@');

            return Ok(new
            {
                available = !exists,
                type = isEmail ? "email" : "mobile",
                value = isEmail ? MaskEmail(emailOrMobile) : MaskMobile(emailOrMobile),
                message = exists
                    ? $"This {(isEmail ? "email" : "mobile number")} is already registered"
                    : $"This {(isEmail ? "email" : "mobile number")} is available",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check availability error for {EmailOrMobile}", emailOrMobile);
            return StatusCode(500, "An error occurred while checking availability");
        }
    }

    /// <summary>
    /// Helper method to mask mobile number for security
    /// </summary>
    private static string MaskMobile(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4)
            return mobile;

        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");

        if (mobile.StartsWith("+20"))
            mobile = mobile[3..];

        if (mobile.Length >= 11)
            return $"{mobile[..3]}***{mobile[^2..]}";

        return $"{mobile[..2]}***{mobile[^1]}";
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