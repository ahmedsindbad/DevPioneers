// ============================================
// File: DevPioneers.Api/Extensions/ApplicationBuilderExtensions.cs (Updated)
// Updated application pipeline configuration
// ============================================
using DevPioneers.Api.Middleware;
using DevPioneers.Application.Common.Interfaces;

namespace DevPioneers.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configure enhanced JWT Authentication middleware pipeline
    /// </summary>
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
    {
        // Add security headers before authentication
        app.UseSecurityHeaders();
        
        // Add ASP.NET Core Authentication middleware
        app.UseAuthentication();
        
        // Add custom JWT middleware for additional processing
        app.UseMiddleware<JwtMiddleware>();
        
        // Add Authorization middleware
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Configure comprehensive security headers
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Prevent clickjacking attacks
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            
            // Prevent MIME type sniffing
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            
            // Enable XSS protection
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            
            // Control referrer information
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Content Security Policy
            context.Response.Headers.Append("Content-Security-Policy", 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';");
            
            // Strict Transport Security (HTTPS only)
            if (context.Request.IsHttps)
            {
                context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }
            
            // Additional security headers
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
            context.Response.Headers.Append("X-Download-Options", "noopen");
            
            // Hide server information
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Append("Server", "DevPioneers-API");

            await next();
        });

        return app;
    }

    /// <summary>
    /// Configure request logging for authorization tracking
    /// </summary>
    public static IApplicationBuilder UseAuthorizationLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();

            if (currentUser.IsAuthenticated)
            {
                logger.LogDebug("Authorized request: {Method} {Path} by User {UserId} ({Email}) from IP {IpAddress}",
                    context.Request.Method,
                    context.Request.Path,
                    currentUser.UserId,
                    currentUser.Email,
                    currentUser.IpAddress);
            }

            await next();
        });

        return app;
    }
}
