using Hangfire.Dashboard;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // للسماح للجميع (في بيئة التطوير فقط)
        return true;

        // يمكنك لاحقًا تقييدها مثلاً حسب المستخدم:
        // var httpContext = context.GetHttpContext();
        // return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}
