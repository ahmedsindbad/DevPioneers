// ============================================
// File: DevPioneers.Application/DependencyInjection.cs (Update)
// Update to register the enhanced authorization behavior
// ============================================
using DevPioneers.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DevPioneers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register Validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register MediatR Pipeline Behaviors (ORDER MATTERS!)
        // 1. Logging behavior (first - to log all requests)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        
        // 2. Validation behavior (before authorization)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        // 3. Enhanced Authorization behavior (replaces the basic one)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(EnhancedAuthorizationBehavior<,>));
        
        // 4. Caching behavior (after authorization)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        
        // 5. Transaction behavior (for commands only)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        
        // 6. Performance behavior (last - to measure total time)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}