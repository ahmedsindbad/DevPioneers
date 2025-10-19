using System.Text;
using DevPioneers.Api.Authorization;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

// File: DevPioneers.Api/Extensions/ServiceCollectionExtensions.cs (Complete)
// Complete implementation of service registration extensions
// ============================================

namespace DevPioneers.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configure JWT Authentication with enhanced settings
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = jwtSettings?.Issuer,
                ValidAudience = jwtSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };

            // Handle JWT from cookie as fallback
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Check for JWT in Authorization header first
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        return Task.CompletedTask;
                    }

                    // Fallback to cookie
                    context.Token = context.Request.Cookies["AccessToken"];
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Authentication required",
                            message = "You must be authenticated to access this resource"
                        });
                        return context.Response.WriteAsync(result);
                    }
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "Access forbidden",
                        message = "You don't have permission to access this resource"
                    });
                    return context.Response.WriteAsync(result);
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configure enhanced authorization policies with custom requirements
    /// </summary>
    public static IServiceCollection AddEnhancedAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // ============================================
            // ROLE-BASED POLICIES
            // ============================================
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Admin"));
            
            options.AddPolicy("ManagerOrAdmin", policy => 
                policy.RequireRole("Manager", "Admin"));
            
            options.AddPolicy("UserOrAbove", policy => 
                policy.RequireRole("User", "Manager", "Admin"));

            // ============================================
            // FEATURE-SPECIFIC POLICIES
            // ============================================
            options.AddPolicy("CanManageSubscriptions", policy => 
                policy.RequireRole("Admin", "Manager"));
            
            options.AddPolicy("CanAccessWallet", policy => 
                policy.RequireAuthenticatedUser());
            
            options.AddPolicy("CanViewReports", policy => 
                policy.RequireRole("Admin", "Manager"));
            
            options.AddPolicy("CanManagePayments", policy => 
                policy.RequireRole("Admin", "Manager"));
            
            options.AddPolicy("CanManageUsers", policy => 
                policy.RequireRole("Admin"));
            
            options.AddPolicy("CanViewAuditTrail", policy => 
                policy.RequireRole("Admin"));

            // ============================================
            // RESOURCE-SPECIFIC POLICIES
            // ============================================
            options.AddPolicy("SubscriptionRead", policy => 
                policy.RequireRole("User", "Manager", "Admin"));
            
            options.AddPolicy("SubscriptionWrite", policy => 
                policy.RequireRole("Manager", "Admin"));
            
            options.AddPolicy("SubscriptionDelete", policy => 
                policy.RequireRole("Admin"));

            options.AddPolicy("PaymentRead", policy => 
                policy.RequireRole("Manager", "Admin"));
            
            options.AddPolicy("PaymentWrite", policy => 
                policy.RequireRole("Manager", "Admin"));
            
            options.AddPolicy("PaymentDelete", policy => 
                policy.RequireRole("Admin"));

            options.AddPolicy("UserRead", policy => 
                policy.RequireRole("Manager", "Admin"));
            
            options.AddPolicy("UserWrite", policy => 
                policy.RequireRole("Admin"));
            
            options.AddPolicy("UserDelete", policy => 
                policy.RequireRole("Admin"));

            options.AddPolicy("ReportRead", policy => 
                policy.RequireRole("Manager", "Admin"));
            
            options.AddPolicy("ReportWrite", policy => 
                policy.RequireRole("Admin"));
            
            options.AddPolicy("ReportDelete", policy => 
                policy.RequireRole("Admin"));

            // ============================================
            // TIME-BASED POLICIES
            // ============================================
            options.AddPolicy("BusinessHoursOnly", policy =>
                policy.RequireAssertion(context =>
                {
                    var now = DateTime.UtcNow.TimeOfDay;
                    return now >= TimeSpan.FromHours(9) && now <= TimeSpan.FromHours(17);
                }));

            options.AddPolicy("MaintenanceWindowOnly", policy =>
                policy.RequireAssertion(context =>
                {
                    var now = DateTime.UtcNow.TimeOfDay;
                    return now >= TimeSpan.FromHours(2) && now <= TimeSpan.FromHours(4);
                }));

            // ============================================
            // IP-BASED POLICIES
            // ============================================
            options.AddPolicy("AdminWorkstationOnly", policy =>
                policy.RequireAssertion(context =>
                {
                    var httpContext = context.Resource as HttpContext;
                    if (httpContext == null) return false;
                    
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
                    var allowedIps = new[] { "192.168.1.100", "10.0.0.5" };
                    
                    return allowedIps.Contains(clientIp);
                }));

            // ============================================
            // CUSTOM REQUIREMENT POLICIES
            // ============================================
            options.AddPolicy("OwnerOrAdmin", policy =>
                policy.AddRequirements(new OwnerOrAdminRequirement()));

            options.AddPolicy("SensitiveDataAccess", policy =>
                policy.RequireRole("Admin")
                      .RequireAssertion(context =>
                      {
                          // Must be Admin + during business hours + from allowed IP
                          var httpContext = context.Resource as HttpContext;
                          if (httpContext == null) return false;

                          var now = DateTime.UtcNow.TimeOfDay;
                          var isBusinessHours = now >= TimeSpan.FromHours(9) && now <= TimeSpan.FromHours(17);

                          var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
                          var allowedIps = new[] { "192.168.1.100" };
                          var isAllowedIp = allowedIps.Contains(clientIp);

                          return isBusinessHours && isAllowedIp;
                      }));

            // ============================================
            // COMBINED POLICIES
            // ============================================
            options.AddPolicy("SuperAdmin", policy =>
                policy.RequireRole("Admin")
                      .RequireAssertion(context =>
                      {
                          var httpContext = context.Resource as HttpContext;
                          if (httpContext == null) return false;

                          var userService = httpContext.RequestServices.GetService<ICurrentUserService>();
                          if (userService == null) return false;

                          // Super admin must be from specific IP and have specific email domain
                          var clientIp = userService.IpAddress;
                          var email = userService.Email;

                          var allowedIps = new[] { "192.168.1.100" };
                          var isAllowedIp = allowedIps.Contains(clientIp);
                          var isSuperAdminEmail = email?.EndsWith("@devpioneers.com") == true;

                          return isAllowedIp && isSuperAdminEmail;
                      }));
        });

        return services;
    }

    /// <summary>
    /// Add CORS policies for enhanced security
    /// </summary>
    public static IServiceCollection AddEnhancedCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
        
        services.AddCors(options =>
        {
            options.AddPolicy("Development", builder =>
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());

            options.AddPolicy("Production", builder =>
                builder.WithOrigins(allowedOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(5)));

            options.AddPolicy("RestrictedApi", builder =>
                builder.WithOrigins("https://admin.devpioneers.com")
                       .WithMethods("GET", "POST")
                       .WithHeaders("Authorization", "Content-Type")
                       .AllowCredentials());
        });

        return services;
    }
}