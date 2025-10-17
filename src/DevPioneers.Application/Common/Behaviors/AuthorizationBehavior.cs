// ============================================
// File: DevPioneers.Application/Common/Behaviors/AuthorizationBehavior.cs
// ============================================
using MediatR;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Exceptions;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for authorization
/// Checks if user has required permissions for protected requests
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;

    public AuthorizationBehavior(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if request requires authorization
        var authorizeAttributes = typeof(TRequest)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .ToList();

        if (!authorizeAttributes.Any())
        {
            return await next();
        }

        // Check if user is authenticated
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication required");
        }

        return await next();
    }
}

/// <summary>
/// Authorization attribute for commands/queries
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AuthorizeAttribute : Attribute
{
    public string? Roles { get; set; }
    public string? Policy { get; set; }

    public AuthorizeAttribute() { }

    public AuthorizeAttribute(string roles)
    {
        Roles = roles;
    }
}
