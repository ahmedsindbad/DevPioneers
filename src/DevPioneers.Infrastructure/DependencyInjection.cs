// ============================================
// File: DevPioneers.Infrastructure/DependencyInjection.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        // Register DateTime Service
        services.AddScoped<IDateTime, DateTimeService>();

        // More services will be added in Phase 4
        // - JwtTokenService
        // - RefreshTokenService
        // - OtpService
        // - EmailService
        // - PaymobService
        // - CacheService

        return services;
    }
}