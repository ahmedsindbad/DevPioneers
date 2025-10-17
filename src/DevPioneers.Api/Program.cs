// ============================================
// File: DevPioneers.Api/Program.cs
// Main entry point for the application
// ============================================

using DevPioneers.Application;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure;
using DevPioneers.Infrastructure.Services;
using DevPioneers.Persistence;
using System.Net;

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
builder.Services.AddSwaggerGen();

// ============================================
// Add Application Layer (MediatR, Validation, Behaviors)
// ============================================
builder.Services.AddApplication();

// ============================================
// Add Persistence Layer (DbContext, Repositories)
// ============================================
builder.Services.AddPersistence(builder.Configuration);

// ============================================
// Add Infrastructure Services (Email, DateTime, etc.)
// ============================================
builder.Services.AddInfrastructure(builder.Configuration);

// Temporary: Override with Mock CurrentUserService for migrations only
// This will be removed once JWT authentication is fully implemented
builder.Services.AddScoped<ICurrentUserService>(provider => 
    new MockCurrentUserService());

// ============================================
// Configure CORS
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
});

// ============================================
// Build the app
// ============================================
var app = builder.Build();

// ============================================
// Configure the HTTP request pipeline
// ============================================

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use HTTPS Redirection
app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// Use Authorization
app.UseAuthorization();

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
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw; // إعادة إرسال الخطأ للتعامل معه بشكل صحيح
    }
}

// ============================================
// Run the application
// ============================================
app.Run();

// ============================================
// Mock CurrentUserService for Design-Time (Migrations)
// This will be replaced with real implementation in Phase 4
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