// ============================================
// Custom Authorization Requirements
// ============================================

using DevPioneers.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DevPioneers.Api.Authorization;

/// <summary>
/// Custom authorization requirement for owner or admin access
/// </summary>
public class OwnerOrAdminRequirement : IAuthorizationRequirement
{
    public string UserIdPropertyName { get; }

    public OwnerOrAdminRequirement(string userIdPropertyName = "UserId")
    {
        UserIdPropertyName = userIdPropertyName;
    }
}

/// <summary>
/// Handler for OwnerOrAdminRequirement
/// </summary>
public class OwnerOrAdminHandler : AuthorizationHandler<OwnerOrAdminRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public OwnerOrAdminHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOrAdminRequirement requirement)
    {
        // Admin can access everything
        if (_currentUserService.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user owns the resource
        if (context.Resource != null)
        {
            var resourceType = context.Resource.GetType();
            var userIdProperty = resourceType.GetProperty(requirement.UserIdPropertyName);
            
            if (userIdProperty != null)
            {
                var resourceUserId = userIdProperty.GetValue(context.Resource);
                if (resourceUserId != null && resourceUserId.Equals(_currentUserService.UserId))
                {
                    context.Succeed(requirement);
                }
            }
        }

        return Task.CompletedTask;
    }
}
