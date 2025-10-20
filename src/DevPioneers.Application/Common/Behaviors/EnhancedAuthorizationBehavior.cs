// ============================================
// File: DevPioneers.Application/Common/Behaviors/EnhancedAuthorizationBehavior.cs
// Enhanced Authorization Behavior that replaces the existing AuthorizationBehavior
// ============================================
using MediatR;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Exceptions;
using DevPioneers.Application.Common.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// Enhanced MediatR pipeline behavior for comprehensive authorization
/// Supports role-based, policy-based, ownership-based, time-based, and IP-based authorization
/// </summary>
public class EnhancedAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<EnhancedAuthorizationBehavior<TRequest, TResponse>> _logger;
    private readonly IDateTime _dateTime;

    public EnhancedAuthorizationBehavior(
        ICurrentUserService currentUserService,
        ILogger<EnhancedAuthorizationBehavior<TRequest, TResponse>> logger,
        IDateTime dateTime)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var requestName = requestType.Name;

        // Check for AllowAnonymous attribute first
        if (HasAttribute<AllowAnonymousAttribute>(requestType))
        {
            _logger.LogDebug("Anonymous access allowed for {RequestName}", requestName);
            return await next();
        }

        // Get all authorization attributes
        var authorizationAttributes = GetAuthorizationAttributes(requestType);

        // If no authorization attributes, allow access
        if (!authorizationAttributes.Any())
        {
            return await next();
        }

        // Check if user is authenticated first
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning("Unauthenticated access attempt to {RequestName} from IP: {IpAddress}", 
                requestName, _currentUserService.IpAddress);
            throw new UnauthorizedException("Authentication required to access this resource");
        }

        // Perform all authorization checks
        await PerformAuthorizationChecks(request, authorizationAttributes, requestName);

        _logger.LogDebug("Authorization successful for {RequestName} by user {UserId}", 
            requestName, _currentUserService.UserId);

        return await next();
    }

    /// <summary>
    /// Perform all authorization checks based on attributes
    /// </summary>
    private async Task PerformAuthorizationChecks(
        TRequest request, 
        IEnumerable<Attribute> authorizationAttributes, 
        string requestName)
    {
        foreach (var attribute in authorizationAttributes)
        {
            switch (attribute)
            {
                case RequireAuthorizationAttribute authAttr:
                    await CheckRoleAuthorization(authAttr, requestName);
                    break;

                case RequirePermissionAttribute permAttr:
                    await CheckPermissionAuthorization(permAttr, requestName);
                    break;

                case RequireResourceAccessAttribute resourceAttr:
                    await CheckResourceAuthorization(resourceAttr, requestName);
                    break;

                case RequireOwnershipAttribute ownershipAttr:
                    await CheckOwnershipAuthorization(request, ownershipAttr, requestName);
                    break;

                case RequireTimeWindowAttribute timeAttr:
                    CheckTimeWindowAuthorization(timeAttr, requestName);
                    break;

                case RequireIpAddressAttribute ipAttr:
                    CheckIpAddressAuthorization(ipAttr, requestName);
                    break;

                // case RequirePolicyAttribute policyAttr:
                //     await CheckPolicyAuthorization(policyAttr, requestName);
                //     break;
            }
        }
    }

    /// <summary>
    /// Check role-based authorization
    /// </summary>
    private Task CheckRoleAuthorization(RequireAuthorizationAttribute attribute, string requestName)
    {
        if (string.IsNullOrEmpty(attribute.Roles))
            return Task.CompletedTask;

        var requiredRoles = attribute.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(r => r.Trim())
                                         .ToList();

        var userRoles = _currentUserService.Roles.ToList();

        bool hasAccess;
        if (attribute.RequireAllRoles)
        {
            // User must have ALL required roles
            hasAccess = requiredRoles.All(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }
        else
        {
            // User must have ANY of the required roles
            hasAccess = requiredRoles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        if (!hasAccess)
        {
            _logger.LogWarning("Access denied to {RequestName} for user {UserId}. Required roles: {RequiredRoles}, User roles: {UserRoles}",
                requestName, _currentUserService.UserId, string.Join(", ", requiredRoles), string.Join(", ", userRoles));

            throw new ForbiddenException($"Access denied. Required roles: {string.Join(" or ", requiredRoles)}");
        }

        _logger.LogDebug("Role authorization successful for {RequestName}. User roles: {UserRoles}",
            requestName, string.Join(", ", userRoles));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check permission-based authorization
    /// </summary>
    private Task CheckPermissionAuthorization(RequirePermissionAttribute attribute, string requestName)
    {
        // This would typically check against a permissions system
        // For now, we'll implement basic permission checks based on roles
        var userRoles = _currentUserService.Roles.ToList();

        var hasPermission = attribute.RequireAllPermissions
            ? attribute.Permissions.All(p => HasPermission(p, userRoles))
            : attribute.Permissions.Any(p => HasPermission(p, userRoles));

        if (!hasPermission)
        {
            _logger.LogWarning("Permission denied to {RequestName} for user {UserId}. Required permissions: {RequiredPermissions}",
                requestName, _currentUserService.UserId, string.Join(", ", attribute.Permissions));

            throw new ForbiddenException($"Access denied. Required permissions: {string.Join(" and ", attribute.Permissions)}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check resource-specific authorization
    /// </summary>
    private Task CheckResourceAuthorization(RequireResourceAccessAttribute attribute, string requestName)
    {
        // Resource-based authorization logic
        var userRoles = _currentUserService.Roles.ToList();

        bool hasResourceAccess = attribute.Actions.All(action =>
            HasResourceAccess(attribute.ResourceType, action, userRoles));

        if (!hasResourceAccess)
        {
            _logger.LogWarning("Resource access denied to {RequestName} for user {UserId}. Resource: {ResourceType}, Actions: {Actions}",
                requestName, _currentUserService.UserId, attribute.ResourceType, string.Join(", ", attribute.Actions));

            throw new ForbiddenException($"Access denied to {attribute.ResourceType} resource for actions: {string.Join(", ", attribute.Actions)}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check ownership-based authorization
    /// </summary>
    private Task CheckOwnershipAuthorization(TRequest request, RequireOwnershipAttribute attribute, string requestName)
    {
        var requestType = request.GetType();
        var userIdProperty = requestType.GetProperty(attribute.UserIdPropertyName);

        if (userIdProperty == null)
        {
            _logger.LogError("Property {PropertyName} not found in {RequestType} for ownership check",
                attribute.UserIdPropertyName, requestType.Name);
            throw new ForbiddenException($"Cannot verify ownership: property {attribute.UserIdPropertyName} not found");
        }

        var resourceUserId = userIdProperty.GetValue(request);
        var currentUserId = _currentUserService.UserId;

        if (resourceUserId == null || !resourceUserId.Equals(currentUserId))
        {
            // Allow Admin to bypass ownership checks
            if (!_currentUserService.IsInRole("Admin"))
            {
                _logger.LogWarning("Ownership check failed for {RequestName}. Resource UserId: {ResourceUserId}, Current UserId: {CurrentUserId}",
                    requestName, resourceUserId, currentUserId);

                throw new ForbiddenException("Access denied. You can only access your own resources");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check time window authorization
    /// </summary>
    private void CheckTimeWindowAuthorization(RequireTimeWindowAttribute attribute, string requestName)
    {
        var currentTime = _dateTime.UtcNow.TimeOfDay;
        
        bool isWithinTimeWindow;
        if (attribute.StartTime <= attribute.EndTime)
        {
            // Same day window (e.g., 09:00 to 17:00)
            isWithinTimeWindow = currentTime >= attribute.StartTime && currentTime <= attribute.EndTime;
        }
        else
        {
            // Overnight window (e.g., 22:00 to 06:00)
            isWithinTimeWindow = currentTime >= attribute.StartTime || currentTime <= attribute.EndTime;
        }

        if (!isWithinTimeWindow)
        {
            _logger.LogWarning("Time window access denied to {RequestName} for user {UserId}. Current time: {CurrentTime}, Allowed: {StartTime}-{EndTime}", 
                requestName, _currentUserService.UserId, currentTime, attribute.StartTime, attribute.EndTime);
            
            throw new ForbiddenException($"Access denied. Resource only available between {attribute.StartTime} and {attribute.EndTime}");
        }
    }

    /// <summary>
    /// Check IP address authorization
    /// </summary>
    private void CheckIpAddressAuthorization(RequireIpAddressAttribute attribute, string requestName)
    {
        var clientIp = _currentUserService.IpAddress;
        
        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogWarning("Cannot determine client IP for {RequestName}", requestName);
            throw new ForbiddenException("Access denied. Cannot determine client IP address");
        }

        bool isAllowedIp = attribute.AllowedIpRanges.Any(range => IsIpInRange(clientIp, range));

        if (!isAllowedIp)
        {
            _logger.LogWarning("IP access denied to {RequestName} for user {UserId}. Client IP: {ClientIp}, Allowed ranges: {AllowedRanges}", 
                requestName, _currentUserService.UserId, clientIp, string.Join(", ", attribute.AllowedIpRanges));
            
            throw new ForbiddenException($"Access denied from IP address: {clientIp}");
        }
    }

    /// <summary>
    /// Check policy-based authorization
    /// </summary>
    private Task CheckPolicyAuthorization(RequirePolicyAttribute attribute, string requestName)
    {
        // This would integrate with ASP.NET Core's policy system
        // For now, implement basic policy checks
        bool hasPolicy = CheckPolicy(attribute.Policy);

        if (!hasPolicy)
        {
            _logger.LogWarning("Policy access denied to {RequestName} for user {UserId}. Required policy: {Policy}",
                requestName, _currentUserService.UserId, attribute.Policy);

            throw new ForbiddenException($"Access denied. Required policy: {attribute.Policy}");
        }

        return Task.CompletedTask;
    }

    #region Helper Methods

    /// <summary>
    /// Get all authorization attributes from request type
    /// </summary>
    private static IEnumerable<Attribute> GetAuthorizationAttributes(Type requestType)
    {
        return requestType.GetCustomAttributes()
            .Where(attr => attr is RequireAuthorizationAttribute ||
                          attr is RequirePermissionAttribute ||
                          attr is RequireResourceAccessAttribute ||
                          attr is RequireOwnershipAttribute ||
                          attr is RequireTimeWindowAttribute ||
                          attr is RequireIpAddressAttribute ||
                          attr is RequirePolicyAttribute ||
                          // Include the existing AuthorizeAttribute for backward compatibility
                          attr.GetType().Name == "AuthorizeAttribute");
    }

    /// <summary>
    /// Check if type has specific attribute
    /// </summary>
    private static bool HasAttribute<T>(Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>() != null;
    }

    /// <summary>
    /// Check if user has specific permission (role-based implementation)
    /// </summary>
    private static bool HasPermission(string permission, List<string> userRoles)
    {
        // Map permissions to roles - this would typically come from a database
        var permissionRoleMap = new Dictionary<string, string[]>
        {
            ["CanManageUsers"] = ["Admin"],
            ["CanManageSubscriptions"] = ["Admin", "Manager"],
            ["CanViewReports"] = ["Admin", "Manager"],
            ["CanManagePayments"] = ["Admin", "Manager"],
            ["CanAccessWallet"] = ["Admin", "Manager", "User"],
            ["CanViewAuditTrail"] = ["Admin"],
            ["CanManageContent"] = ["Admin", "Manager"],
            ["CanDeleteUsers"] = ["Admin"]
        };

        if (!permissionRoleMap.TryGetValue(permission, out var requiredRoles))
        {
            // If permission not mapped, default to Admin only
            requiredRoles = ["Admin"];
        }

        return requiredRoles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check resource access based on resource type and action
    /// </summary>
    private static bool HasResourceAccess(string resourceType, string action, List<string> userRoles)
    {
        // Resource-action-role mapping
        var resourceAccessMap = new Dictionary<string, Dictionary<string, string[]>>
        {
            ["Subscription"] = new()
            {
                ["Read"] = ["Admin", "Manager", "User"],
                ["Write"] = ["Admin", "Manager"],
                ["Delete"] = ["Admin"]
            },
            ["Payment"] = new()
            {
                ["Read"] = ["Admin", "Manager"],
                ["Write"] = ["Admin", "Manager"],
                ["Delete"] = ["Admin"]
            },
            ["User"] = new()
            {
                ["Read"] = ["Admin", "Manager"],
                ["Write"] = ["Admin"],
                ["Delete"] = ["Admin"]
            },
            ["Report"] = new()
            {
                ["Read"] = ["Admin", "Manager"],
                ["Write"] = ["Admin"],
                ["Delete"] = ["Admin"]
            }
        };

        if (!resourceAccessMap.TryGetValue(resourceType, out var actionMap))
            return false;

        if (!actionMap.TryGetValue(action, out var requiredRoles))
            return false;

        return requiredRoles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if IP is in allowed range (basic implementation)
    /// </summary>
    private static bool IsIpInRange(string clientIp, string allowedRange)
    {
        // Basic IP range checking - in production, use a proper CIDR library
        if (allowedRange == "*" || allowedRange == "all")
            return true;

        if (allowedRange.Contains("/"))
        {
            // CIDR notation - simplified implementation
            var parts = allowedRange.Split('/');
            var networkIp = parts[0];
            // In production, implement proper CIDR checking
            return clientIp.StartsWith(networkIp.Substring(0, networkIp.LastIndexOf('.')));
        }

        return clientIp == allowedRange;
    }

    /// <summary>
    /// Check policy (basic implementation)
    /// </summary>
    private bool CheckPolicy(string? policy)
    {
        if (string.IsNullOrEmpty(policy))
            return true;

        // Map policies to role requirements
        var policyMap = new Dictionary<string, string[]>
        {
            ["AdminOnly"] = ["Admin"],
            ["ManagerOrAdmin"] = ["Manager", "Admin"],
            ["UserOrAbove"] = ["User", "Manager", "Admin"],
            ["CanManageSubscriptions"] = ["Admin", "Manager"],
            ["CanAccessWallet"] = ["User", "Manager", "Admin"],
            ["CanViewReports"] = ["Admin", "Manager"]
        };

        if (!policyMap.TryGetValue(policy, out var requiredRoles))
        {
            // If policy not mapped, default to Admin only
            requiredRoles = ["Admin"];
        }

        var userRoles = _currentUserService.Roles.ToList();
        return requiredRoles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    #endregion
}