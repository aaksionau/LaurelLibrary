using Hangfire.Dashboard;

namespace LaurelLibrary.UI.Services;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access only to authenticated users
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}
