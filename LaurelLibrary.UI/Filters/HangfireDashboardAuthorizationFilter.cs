using System.Security.Claims;
using Hangfire.Dashboard;

namespace LaurelLibrary.UI.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access only to authenticated users with Administrator claim
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check if user has Administrator claim
        return httpContext.User.HasClaim("Administrator", "true")
            || httpContext.User.HasClaim(ClaimTypes.Role, "Administrator");
    }
}
