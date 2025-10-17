// ============================================
// File: DevPioneers.Application/DependencyInjection.cs
// ============================================
using DevPioneers.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DevPioneers.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Add Application layer services to DI container
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
 
            // Add pipeline behaviors (order matters!)
            cfg.AddBehavior<AuthorizationBehavior<,>>();
            cfg.AddBehavior<ValidationBehavior<,>>();
            cfg.AddBehavior<CachingBehavior<,>>();
            cfg.AddBehavior<LoggingBehavior<,>>();
            cfg.AddBehavior<PerformanceBehavior<,>>();
            cfg.AddBehavior<TransactionBehavior<,>>();
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
