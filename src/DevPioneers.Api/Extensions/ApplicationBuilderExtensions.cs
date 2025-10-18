// ============================================
// File: DevPioneers.Api/Extensions/ApplicationBuilderExtensions.cs
// Extension methods for configuring the application pipeline
// ============================================
using DevPioneers.Api.Middleware;
using DevPioneers.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DevPioneers.Api.Extensions;

/// <summary>
/// Extension methods for IApplicationBuilder to configure middleware pipeline
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configure JWT Authentication middleware with proper security headers
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
    /// Configure security headers
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
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' https: data:; " +
                "connect-src 'self' https:; " +
                "frame-ancestors 'none'");
            
            // HSTS for HTTPS connections
            if (context.Request.IsHttps)
            {
                context.Response.Headers.Append("Strict-Transport-Security", 
                    "max-age=31536000; includeSubDomains; preload");
            }
            
            await next();
        });

        return app;
    }

    /// <summary>
    /// Configure CORS policy based on environment
    /// </summary>
    public static IApplicationBuilder UseEnvironmentBasedCors(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsProduction())
        {
            app.UseCors("Production");
        }
        else
        {
            app.UseCors("AllowAll");
        }

        return app;
    }

    /// <summary>
    /// Configure JWT cookie settings for authentication
    /// </summary>
    public static IApplicationBuilder UseJwtCookieSettings(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Configure JWT cookie settings for login responses
            if (context.Request.Path.StartsWithSegments("/api/auth/login") && 
                context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // This is a simple example - in real implementation, you'd parse the response
                // and set cookies after successful login
                // For now, we just continue to the next middleware
            }

            await next();
        });

        return app;
    }

    /// <summary>
    /// Configure request logging middleware
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var startTime = DateTime.UtcNow;
            
            // Log request
            logger.LogInformation("Request: {Method} {Path} from {IpAddress}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString());

            await next();

            // Log response
            var elapsed = DateTime.UtcNow - startTime;
            
            logger.LogInformation("Response: {StatusCode} in {ElapsedMs}ms for {Method} {Path}",
                context.Response.StatusCode,
                elapsed.TotalMilliseconds,
                context.Request.Method,
                context.Request.Path);
        });

        return app;
    }

    /// <summary>
    /// Configure API versioning and swagger based on environment
    /// </summary>
    public static IApplicationBuilder UseSwaggerWithJwtAuth(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevPioneers API v1");
                c.DocumentTitle = "DevPioneers API - JWT Authentication Enabled";
                c.DefaultModelsExpandDepth(-1);
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                
                // JWT Authentication instructions
                c.HeadContent = @"
                    <style>
                        .swagger-ui .topbar { display: none; }
                        .swagger-ui .info .title:after { 
                            content: ' 🔐 JWT Auth Enabled'; 
                            color: #49cc90; 
                            font-size: 0.6em; 
                        }
                    </style>";
            });
        }

        return app;
    }

    /// <summary>
    /// Configure health checks endpoints
    /// </summary>
    public static IApplicationBuilder UseHealthCheckEndpoints(this IApplicationBuilder app)
    {
        // Basic health check
        app.Map("/health", appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                context.Response.ContentType = "application/json";
                
                var healthResponse = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    service = "DevPioneers.Api",
                    version = "1.0.0"
                };
                
                var jsonResponse = JsonSerializer.Serialize(healthResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                await context.Response.WriteAsync(jsonResponse);
            });
        });

        return app;
    }

    /// <summary>
    /// Configure exception handling with proper JWT context
    /// </summary>
    public static IApplicationBuilder UseJwtAwareExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                if (feature?.Error != null)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    
                    // Include user context in error logging if available
                    var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    
                    logger.LogError(feature.Error, 
                        "Unhandled exception occurred for user {UserId} on path {Path}", 
                        userId ?? "anonymous", 
                        context.Request.Path);

                    var response = new
                    {
                        error = "An internal server error occurred",
                        timestamp = DateTime.UtcNow,
                        path = context.Request.Path.Value,
                        user = userId ?? "anonymous"
                    };

                    var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });

                    await context.Response.WriteAsync(jsonResponse);
                }
            });
        });

        return app;
    }

    /// <summary>
    /// Configure comprehensive middleware pipeline for JWT Authentication
    /// </summary>
    public static IApplicationBuilder UseJwtPipeline(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Exception handling (first in pipeline)
        app.UseJwtAwareExceptionHandling();
        
        // Security headers
        app.UseSecurityHeaders();
        
        // HTTPS redirection
        app.UseHttpsRedirection();
        
        // CORS based on environment
        app.UseEnvironmentBasedCors(env);
        
        // Authentication & Authorization
        app.UseAuthentication();
        app.UseMiddleware<JwtMiddleware>();
        app.UseAuthorization();
        
        // Request logging
        app.UseRequestLogging();
        
        // Health checks
        app.UseHealthCheckEndpoints();
        
        return app;
    }
}