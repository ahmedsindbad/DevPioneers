// ============================================
// File: DevPioneers.Api/Middleware/JwtMiddleware.cs
// JWT Authentication Middleware - Handles JWT token validation and user context setup
// ============================================
using DevPioneers.Infrastructure.Services.Auth;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using DevPioneers.Infrastructure.Configurations;
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Api.Middleware;

/// <summary>
/// JWT Middleware for token validation and user context setup
/// Validates JWT tokens and extracts user claims for each request
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(
        RequestDelegate next,
        ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings,
        IWebHostEnvironment environment)
    {
        try
        {
            // Extract JWT token from request
            var token = ExtractTokenFromRequest(context);

            if (!string.IsNullOrEmpty(token))
            {
                await ValidateAndSetUserContextAsync(context, token, jwtTokenService, environment);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JWT Middleware: {Message}", ex.Message);
            await HandleAuthenticationErrorAsync(context, ex, environment);
        }
    }

    /// <summary>
    /// Extract JWT token from Authorization header or cookies
    /// </summary>
    private string? ExtractTokenFromRequest(HttpContext context)
    {
        // Priority 1: Authorization header with Bearer token
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Priority 2: HttpOnly cookie (more secure for web applications)
        if (context.Request.Cookies.TryGetValue("AccessToken", out var cookieToken))
        {
            return cookieToken;
        }

        // Priority 3: Query parameter (for specific scenarios like file downloads)
        var queryToken = context.Request.Query["token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(queryToken))
        {
            return queryToken;
        }

        return null;
    }

    /// <summary>
    /// Validate JWT token and set user context
    /// </summary>
    private async Task ValidateAndSetUserContextAsync(
        HttpContext context, 
        string token, 
        IJwtTokenService jwtTokenService,
        IWebHostEnvironment environment)
    {
        try
        {
            // Validate token using JwtTokenService
            var isValidToken =  jwtTokenService.ValidateToken(token);
            
            if (!isValidToken)
            {
                _logger.LogWarning("Invalid JWT token received from IP: {IpAddress}", 
                    context.Connection.RemoteIpAddress?.ToString());
                return;
            }

            // Extract claims from token
            var principal = jwtTokenService.GetPrincipalFromExpiredToken(token);
            
            if (principal == null)
            {
                _logger.LogWarning("Failed to extract claims from JWT token");
                return;
            }

            // Set user context
            context.User = principal;

            // Log successful authentication
            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            
            _logger.LogDebug("JWT authentication successful for User: {UserId}, Email: {Email}", 
                userId, userEmail);

            // Add custom headers for debugging (only in development)
            if (environment.IsDevelopment())
            {
                context.Response.Headers.Append("X-User-Id", userId ?? "unknown");
                context.Response.Headers.Append("X-User-Email", userEmail ?? "unknown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Handle authentication errors
    /// </summary>
    private async Task HandleAuthenticationErrorAsync(
        HttpContext context, 
        Exception ex,
        IWebHostEnvironment environment)
    {
        // Only return error for API endpoints, not for pages or assets
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Unauthorized("Authentication failed");
        
        // Add error details only in development
        if (environment.IsDevelopment())
        {
            response = ApiResponse.Unauthorized($"Authentication failed: {ex.Message}");
        }
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(jsonResponse);
    }
}