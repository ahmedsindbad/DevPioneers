// ============================================
// File: DevPioneers.Api/Program.cs
// Main entry point for the application - Fixed JWT duplication issue
// ============================================

using DevPioneers.Application;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure;
using DevPioneers.Infrastructure.Services;
using DevPioneers.Persistence;
using DevPioneers.Api.Middleware;
using DevPioneers.Api.Filters;
using Microsoft.OpenApi.Models;
using System.Net;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using DevPioneers.Api.Extensions;
using DevPioneers.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DevPioneers.Application.Common.Behaviors;

// Force TLS 1.2 or higher for all connections
#pragma warning disable SYSLIB0014 // Type or member is obsolete
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
#pragma warning restore SYSLIB0014 // Type or member is obsolete

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Add services to the container
// ============================================

// Add Controllers
builder.Services.AddControllers();

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();

// ============================================
// Configure Swagger with JWT Authentication
// ============================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DevPioneers API",
        Version = "v1",
        Description = "API documentation for DevPioneers platform with JWT Authentication",
        Contact = new OpenApiContact
        {
            Name = "DevPioneers Team",
            Email = "support@devpioneers.com"
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});


// Replace the existing authorization configuration with enhanced version
// Replace basic AuthorizationBehavior with enhanced version
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(EnhancedAuthorizationBehavior<,>));

// Register custom authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, OwnerOrAdminHandler>();
// ============================================
// Add Application Layer (MediatR, Validation, Behaviors)
// ============================================
builder.Services.AddApplication();

// ============================================
// Add Persistence Layer (DbContext, Repositories)
// ============================================
builder.Services.AddPersistence(builder.Configuration);

// ============================================
// Add Infrastructure Services (JWT, Email, DateTime, etc.)
// This includes JWT Authentication configuration - DO NOT duplicate
// ============================================
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================
// Remove Mock CurrentUserService since we now have JWT auth
// ============================================
// Comment out the Mock service registration:
// builder.Services.AddScoped<ICurrentUserService>(provider =>
//     new MockCurrentUserService());

// ============================================
// Configure CORS with JWT Authentication considerations
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
        
    // More secure CORS policy for production
    options.AddPolicy("Production",
        policy =>
        {
            policy.WithOrigins(
                    "https://dplawyer.app",
                    "https://dplawyer-asa3cfgnb3hbege6.uaenorth-01.azurewebsites.net")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Important for JWT cookies
        });
});

// ============================================
// Add Hangfire with Authentication
// ============================================
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Parse(builder.Configuration["HangfireSettings:SchedulePollingInterval"]!),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Convert.ToInt32(builder.Configuration["HangfireSettings:WorkerCount"]);
});

// ============================================
// Build the app
// ============================================
var app = builder.Build();

// ============================================
// Configure the HTTP request pipeline
// ============================================

// Enable Swagger in Development and Staging
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevPioneers API v1");
        c.DocumentTitle = "DevPioneers API Documentation";
        c.DefaultModelsExpandDepth(-1); // Disable model expansion by default
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

// ============================================
// Security Headers Middleware (before authentication)
// ============================================
app.Use(async (context, next) =>
{
    // Security headers
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // HSTS for HTTPS
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    await next();
});

// Use HTTPS Redirection
// app.UseHttpsRedirection();

// Use CORS
var corsPolicy = app.Environment.IsProduction() ? "Production" : "AllowAll";
app.UseCors(corsPolicy);

// ============================================
// Authentication & Authorization Pipeline
// Infrastructure already configured JWT, just use the middleware
// ============================================

// Add Authentication middleware (validates JWT tokens) - Already configured in Infrastructure
app.UseAuthentication();

// Add custom JWT middleware for additional processing
app.UseMiddleware<JwtMiddleware>();

// Add Authorization middleware
app.UseAuthorization();
app.UseAuthorizationLogging();
// ============================================
// Hangfire Dashboard with Authentication
// ============================================
var dashboardOptions = new DashboardOptions
{
    Authorization = new[]
    {
        new HangfireDashboardAuthorizationFilter() // Custom authorization filter
    },
    AppPath = "/", // Return to main app when clicking site name
    DashboardTitle = "DevPioneers Background Jobs",
    IsReadOnlyFunc = context => !context.GetHttpContext().User.IsInRole("Admin")
};

app.UseHangfireDashboard("/hangfire", dashboardOptions);

// Map Controllers
app.MapControllers();

// ============================================
// Database Initialization (Apply Migrations)
// ============================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.InitializeDatabaseAsync();
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
        throw;
    }
}

// ============================================
// Application Startup Logging
// ============================================
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("DevPioneers API started successfully");
startupLogger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("JWT Authentication: Enabled");
startupLogger.LogInformation("Hangfire Dashboard: /hangfire");
startupLogger.LogInformation("Swagger UI: /swagger");

// ============================================
// Run the application
// ============================================
app.Run();

// ============================================
// Mock CurrentUserService for Design-Time (kept for backward compatibility)
// This will not be used at runtime since we have the real implementation
// ============================================
public class MockCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
    public string? UserFullName => null;
    public string? Email => null;
    public string? UserEmail => null;
    public IEnumerable<string> Roles => Enumerable.Empty<string>();
    public IEnumerable<string> UserRoles => Enumerable.Empty<string>();
    public bool IsAuthenticated => false;
    public string? IpAddress => null;
    public string? UserAgent => null;
    public string? RequestPath => null;
    public string? HttpMethod => null;
    public bool IsInRole(string role) => false;
}