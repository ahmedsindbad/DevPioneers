// ============================================
// File: DevPioneers.Persistence/DependencyInjection.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Persistence.Contexts;
using DevPioneers.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevPioneers.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Add Persistence layer services to DI container
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register AuditInterceptor
        services.AddScoped<AuditInterceptor>();

        // Add Entity Framework
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });

            // Performance optimizations
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

#if DEBUG
            // Enable detailed errors and sensitive data logging in development
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(
                Console.WriteLine,
                new[] { DbLoggerCategory.Database.Command.Name },
                Microsoft.Extensions.Logging.LogLevel.Information);
#endif

            // Add interceptors (This requires the AuditInterceptor to be registered first)
            var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
            options.AddInterceptors(auditInterceptor);
        });

        // Register IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    /// <summary>
    /// Initialize database (apply migrations, seed data)
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Apply pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Database migrations applied successfully.");
            }

            // Seed data if needed
            await SeedDataAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ An error occurred while migrating the database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Seed initial data
    /// </summary>
    private static async Task SeedDataAsync(ApplicationDbContext context)
    {
        try
        {
            // Check if roles already exist
            if (!await context.Roles.AnyAsync())
            {
                // Roles will be seeded via RoleConfiguration.HasData()
                Console.WriteLine("ℹ️ Roles will be seeded via Entity Configuration.");
            }

            // Check if subscription plans already exist
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                // SubscriptionPlans will be seeded via SubscriptionPlanConfiguration.HasData()
                Console.WriteLine("ℹ️ Subscription plans will be seeded via Entity Configuration.");
            }

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Initial data seeding completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Warning: Could not seed data: {ex.Message}");
            // Don't throw here - seeding errors shouldn't prevent app startup
        }
    }
}
