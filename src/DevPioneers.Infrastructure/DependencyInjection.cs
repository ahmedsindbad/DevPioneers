// ============================================
// File: DevPioneers.Infrastructure/DependencyInjection.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure.Services;
using DevPioneers.Infrastructure.Services.Payment;
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
        // Register HTTP Context Accessor (required for CurrentUserService)
        services.AddHttpContextAccessor();

        // Register DateTime Service
        services.AddScoped<IDateTime, DateTimeService>();
        services.AddHttpClient("PaymobClient", client =>
        {
            // لا تضف Authorization هنا لأننا نحصل على التوكن داخل الخدمة
            client.Timeout = TimeSpan.FromSeconds(30);
            client.BaseAddress = new Uri(configuration.GetValue<string>("PaymobSettings:BaseUrl") ?? "https://accept.paymob.com");
        });

        services.AddScoped<IPaymentService, PaymobService>();


        // Register CurrentUserService (real implementation)
        // This will be used instead of MockCurrentUserService once JWT auth is implemented
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register Email Service (Mock for development, real implementation later)
        services.AddScoped<IEmailService, MockEmailService>();

        // More services will be added in Phase 4
        // - JwtTokenService
        // - RefreshTokenService
        // - OtpService
        // - PaymobService
        // - CacheService

        return services;
    }
}