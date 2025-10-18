// ============================================
// File: DevPioneers.Api/Filters/HangfireDashboardAuthorizationFilter.cs
// Authorization filter for Hangfire Dashboard
// ============================================
using Hangfire.Dashboard;

namespace DevPioneers.Api.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard access
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Allow access in development environment
        var environment = httpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (environment?.IsDevelopment() == true)
        {
            return true;
        }
        
        // In production, require Admin role
        return httpContext.User?.IsInRole("Admin") == true;
    }
}