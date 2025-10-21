// ============================================
// File: DevPioneers.Infrastructure/DependencyInjection.cs
// Updated version with JWT Authentication Services registration
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure.Services;
using DevPioneers.Infrastructure.Services.Payment;
using DevPioneers.Infrastructure.Services.Auth;
using DevPioneers.Infrastructure.Services.Cache;
using DevPioneers.Infrastructure.Services.BackgroundJobs;
using DevPioneers.Infrastructure.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace DevPioneers.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Add Infrastructure layer services to DI container
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register HTTP Context Accessor (required for CurrentUserService)
        services.AddHttpContextAccessor();

        // ============================================
        // JWT Configuration and Services
        // ============================================
        
        // Bind JWT settings from configuration
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        
        // Validate JWT settings
        if (jwtSettings == null)
        {
            throw new InvalidOperationException("JWT settings are not configured properly. Please check appsettings.json");
        }

        if (string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is required. Please check JwtSettings:SecretKey in appsettings.json");
        }

        if (string.IsNullOrEmpty(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("JWT Issuer is required. Please check JwtSettings:Issuer in appsettings.json");
        }

        if (string.IsNullOrEmpty(jwtSettings.Audience))
        {
            throw new InvalidOperationException("JWT Audience is required. Please check JwtSettings:Audience in appsettings.json");
        }

        // Register JWT-related services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Add JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !configuration.GetValue<bool>("JwtSettings:AllowHttpInDevelopment");
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes),
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // JWT Events for custom handling
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();
                    
                    logger.LogWarning("JWT Authentication failed: {Exception}", context.Exception.Message);
                    
                    // Add custom headers for debugging in development
                    var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
                    if (env?.IsDevelopment() == true)
                    {
                        context.Response.Headers["X-Auth-Error"] = context.Exception.Message;
                    }
                    
                    return Task.CompletedTask;
                },
                
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();
                    
                    var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    logger.LogDebug("JWT Token validated successfully for User: {UserId}", userId);
                    
                    return Task.CompletedTask;
                },
                
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();
                    
                    logger.LogInformation("JWT Authentication challenge triggered for path: {Path}", 
                        context.Request.Path);
                    
                    return Task.CompletedTask;
                },

                OnMessageReceived = context =>
                {
                    // Support token from cookie as well
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        context.Token = context.Request.Cookies["AccessToken"];
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

        // Add Authorization policies
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                
            // Role-based policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
            options.AddPolicy("UserOrAbove", policy => policy.RequireRole("User", "Manager", "Admin"));
            
            // Custom policies for specific features
            options.AddPolicy("CanManageSubscriptions", policy => 
                policy.RequireRole("Admin", "Manager"));
            options.AddPolicy("CanAccessWallet", policy => 
                policy.RequireAuthenticatedUser());
            options.AddPolicy("CanViewReports", policy => 
                policy.RequireRole("Admin", "Manager"));
        });

        // ============================================
        // Other Infrastructure Services
        // ============================================

        // Register DateTime Service
        services.AddScoped<IDateTime, DateTimeService>();

        // HTTP Client for Paymob
        services.AddHttpClient("PaymobClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.BaseAddress = new Uri(configuration.GetValue<string>("PaymobSettings:BaseUrl") ?? "https://accept.paymob.com");
        });

        services.AddScoped<IPaymentService, PaymobService>();

        // Register CurrentUserService (real implementation)
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ============================================
        // Redis Cache Service
        // ============================================

        // Configure Redis connection
        var redisConnectionString = configuration.GetValue<string>("RedisSettings:ConnectionString");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // Register Redis connection multiplexer as singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                configurationOptions.AbortOnConnectFail = false;
                configurationOptions.ConnectTimeout = 5000;
                configurationOptions.SyncTimeout = 5000;
                configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);

                var logger = sp.GetRequiredService<ILogger<ConnectionMultiplexer>>();

                try
                {
                    var connection = ConnectionMultiplexer.Connect(configurationOptions);

                    connection.ConnectionFailed += (sender, args) =>
                    {
                        logger.LogError("Redis connection failed: {EndPoint} - {FailureType}",
                            args.EndPoint, args.FailureType);
                    };

                    connection.ConnectionRestored += (sender, args) =>
                    {
                        logger.LogInformation("Redis connection restored: {EndPoint}",
                            args.EndPoint);
                    };

                    connection.ErrorMessage += (sender, args) =>
                    {
                        logger.LogError("Redis error: {Message}", args.Message);
                    };

                    logger.LogInformation("Redis connection established successfully");
                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to connect to Redis. Cache service will be unavailable.");
                    throw;
                }
            });

            // Register Redis cache service
            services.AddScoped<ICacheService, RedisService>();
        }
        else
        {
            // Redis connection string not configured - cache service will be unavailable
            // Note: Logger not available at this point in service registration
        }

        // ============================================
        // Email Service
        // ============================================

        // Configure Email settings
        services.Configure<EmailSettings>(
            configuration.GetSection(EmailSettings.SectionName));

        // Register Email Service (Use SMTP for production, Mock for development)
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>();
        var environment = services.BuildServiceProvider().GetService<IWebHostEnvironment>();

        if (environment?.IsProduction() == true && emailSettings?.EnableEmailSending == true)
        {
            // Use real SMTP email service in production
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            // Use mock email service in development
            services.AddScoped<IEmailService, MockEmailService>();
        }

        // ============================================
        // OTP Service
        // ============================================

        // Configure OTP settings
        services.Configure<OtpSettings>(
            configuration.GetSection(OtpSettings.SectionName));

        // Register OTP service
        services.AddScoped<IOtpService, OtpService>();

        // ============================================
        // Background Jobs (Hangfire)
        // ============================================

        // Register background job services
        services.AddScoped<IExpireSubscriptionsJob, ExpireSubscriptionsJob>();
        services.AddScoped<IReconcilePaymentsJob, ReconcilePaymentsJob>();
        services.AddScoped<ISendEmailJob, SendEmailJob>();
        services.AddScoped<ICleanOldAuditTrailJob, CleanOldAuditTrailJob>();

        return services;
    }

    /// <summary>
    /// Validate JWT settings during startup
    /// </summary>
    private static void ValidateJwtSettings(JwtSettings jwtSettings)
    {
        if (jwtSettings == null)
            throw new ArgumentNullException(nameof(jwtSettings), "JWT settings cannot be null");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new ArgumentException("JWT SecretKey is required", nameof(jwtSettings.SecretKey));

        if (jwtSettings.SecretKey.Length < 32)
            throw new ArgumentException("JWT SecretKey must be at least 32 characters long", nameof(jwtSettings.SecretKey));

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
            throw new ArgumentException("JWT Issuer is required", nameof(jwtSettings.Issuer));

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
            throw new ArgumentException("JWT Audience is required", nameof(jwtSettings.Audience));

        if (jwtSettings.AccessTokenExpirationMinutes <= 0)
            throw new ArgumentException("AccessTokenExpirationMinutes must be greater than 0", nameof(jwtSettings.AccessTokenExpirationMinutes));

        if (jwtSettings.RefreshTokenExpirationDays <= 0)
            throw new ArgumentException("RefreshTokenExpirationDays must be greater than 0", nameof(jwtSettings.RefreshTokenExpirationDays));
    }
}