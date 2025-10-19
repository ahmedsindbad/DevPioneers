// ============================================
// File: DevPioneers.Application/Common/Attributes/AuthorizationAttributes.cs
// Enhanced Authorization Attributes for Role-based and Policy-based Authorization
// ============================================
using System;

namespace DevPioneers.Application.Common.Attributes;

/// <summary>
/// Base authorization attribute for commands/queries
/// Extends the existing AuthorizeAttribute with enhanced functionality
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequireAuthorizationAttribute : Attribute
{
    public string? Roles { get; set; }
    public string? Policy { get; set; }
    public bool RequireAllRoles { get; set; } = false;

    public RequireAuthorizationAttribute() { }

    public RequireAuthorizationAttribute(string roles)
    {
        Roles = roles;
    }

    public RequireAuthorizationAttribute(string roles, bool requireAllRoles)
    {
        Roles = roles;
        RequireAllRoles = requireAllRoles;
    }
}

/// <summary>
/// Attribute to require Admin role only
/// Usage: [RequireAdminRole]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAdminRoleAttribute : RequireAuthorizationAttribute
{
    public RequireAdminRoleAttribute() : base("Admin") { }
}

/// <summary>
/// Attribute to require Manager role or higher (Manager, Admin)
/// Usage: [RequireManagerRole]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireManagerRoleAttribute : RequireAuthorizationAttribute
{
    public RequireManagerRoleAttribute() : base("Manager,Admin") { }
}

/// <summary>
/// Attribute to require User role or higher (User, Manager, Admin)
/// Usage: [RequireUserRole]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireUserRoleAttribute : RequireAuthorizationAttribute
{
    public RequireUserRoleAttribute() : base("User,Manager,Admin") { }
}

/// <summary>
/// Attribute for subscription management permissions
/// Usage: [RequireSubscriptionManagement]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireSubscriptionManagementAttribute : RequireAuthorizationAttribute
{
    public RequireSubscriptionManagementAttribute() : base("Admin,Manager") { }
}

/// <summary>
/// Attribute for wallet access permissions
/// Usage: [RequireWalletAccess]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireWalletAccessAttribute : RequireAuthorizationAttribute
{
    public RequireWalletAccessAttribute() : base("User,Manager,Admin") { }
}

/// <summary>
/// Attribute for report viewing permissions
/// Usage: [RequireReportAccess]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireReportAccessAttribute : RequireAuthorizationAttribute
{
    public RequireReportAccessAttribute() : base("Admin,Manager") { }
}

/// <summary>
/// Attribute for audit trail access permissions
/// Usage: [RequireAuditAccess]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAuditAccessAttribute : RequireAuthorizationAttribute
{
    public RequireAuditAccessAttribute() : base("Admin") { }
}

/// <summary>
/// Attribute for payment management permissions
/// Usage: [RequirePaymentManagement]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequirePaymentManagementAttribute : RequireAuthorizationAttribute
{
    public RequirePaymentManagementAttribute() : base("Admin,Manager") { }
}

/// <summary>
/// Attribute for user management permissions
/// Usage: [RequireUserManagement]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireUserManagementAttribute : RequireAuthorizationAttribute
{
    public RequireUserManagementAttribute() : base("Admin") { }
}

/// <summary>
/// Attribute to require specific policy
/// Usage: [RequirePolicy("PolicyName")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequirePolicyAttribute : RequireAuthorizationAttribute
{
    public RequirePolicyAttribute(string policyName)
    {
        Policy = policyName;
    }
}

/// <summary>
/// Attribute to allow anonymous access (bypasses authorization)
/// Usage: [AllowAnonymous]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AllowAnonymousAttribute : Attribute
{
}

/// <summary>
/// Attribute for custom permission checks
/// Usage: [RequirePermission("CanEditContent", "CanDeleteUsers")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    public string[] Permissions { get; }
    public bool RequireAllPermissions { get; set; } = false;

    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }
}

/// <summary>
/// Attribute for resource-specific authorization
/// Usage: [RequireResourceAccess("Subscription", "Read,Write")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireResourceAccessAttribute : Attribute
{
    public string ResourceType { get; }
    public string[] Actions { get; }

    public RequireResourceAccessAttribute(string resourceType, string actions)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        Actions = actions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? 
                 throw new ArgumentNullException(nameof(actions));
    }
}

/// <summary>
/// Attribute for ownership-based authorization
/// Usage: [RequireOwnership("UserId")] - checks if current user owns the resource
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireOwnershipAttribute : Attribute
{
    public string UserIdPropertyName { get; }

    public RequireOwnershipAttribute(string userIdPropertyName = "UserId")
    {
        UserIdPropertyName = userIdPropertyName ?? throw new ArgumentNullException(nameof(userIdPropertyName));
    }
}

/// <summary>
/// Attribute for time-based authorization
/// Usage: [RequireTimeWindow("09:00", "17:00")] - only allow during business hours
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireTimeWindowAttribute : Attribute
{
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public string TimeZone { get; set; } = "UTC";

    public RequireTimeWindowAttribute(string startTime, string endTime)
    {
        if (!TimeSpan.TryParse(startTime, out var start))
            throw new ArgumentException("Invalid start time format", nameof(startTime));
        
        if (!TimeSpan.TryParse(endTime, out var end))
            throw new ArgumentException("Invalid end time format", nameof(endTime));

        StartTime = start;
        EndTime = end;
    }
}

/// <summary>
/// Attribute for IP-based authorization
/// Usage: [RequireIpAddress("192.168.1.0/24", "10.0.0.0/8")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireIpAddressAttribute : Attribute
{
    public string[] AllowedIpRanges { get; }

    public RequireIpAddressAttribute(params string[] allowedIpRanges)
    {
        AllowedIpRanges = allowedIpRanges ?? throw new ArgumentNullException(nameof(allowedIpRanges));
    }
}