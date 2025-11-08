using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Identity;

namespace LaurelLibrary.UI.Middleware;

public class SubscriptionCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionCheckMiddleware> _logger;

    public SubscriptionCheckMiddleware(
        RequestDelegate next,
        ILogger<SubscriptionCheckMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        // Skip subscription check for certain paths
        if (ShouldSkipSubscriptionCheck(context))
        {
            await _next(context);
            return;
        }

        // Only check for authenticated users
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var subscriptionService =
                scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

            // Get user ID from claims
            var userId =
                context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst("http://schemas.xmlsoap.org/wsdl/")?.Value
                ?? context
                    .User.FindFirst(
                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
                    )
                    ?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                await _next(context);
                return;
            }

            // Check if user has a valid subscription
            var hasValidSubscription = await subscriptionService.HasValidSubscriptionAsync(userId);

            if (!hasValidSubscription)
            {
                _logger.LogInformation(
                    "User {UserId} does not have a valid subscription. Redirecting to subscription page.",
                    userId
                );

                // Redirect to subscription page with a message
                context.Response.Redirect(
                    "/Administration/Subscriptions?redirected=subscription_required"
                );
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during subscription check");
            // Continue processing on error to avoid blocking the application
        }

        await _next(context);
    }

    private static bool ShouldSkipSubscriptionCheck(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip subscription check for these paths
        var skipPaths = new[]
        {
            "/administration/subscriptions", // Subscription management page
            "/administration/libraries", // Library management
            "/administration/libraries/create", // Library creation
            "/account", // Account management
            "/identity", // Identity pages
            "/signin-microsoft", // Microsoft auth callback
            "/health", // Health check
            "/error", // Error pages
            "/_", // Framework internal paths
            "/api", // API endpoints
            "/stripe", // Stripe webhooks
            "/css", // Static resources
            "/js", // Static resources
            "/lib", // Static resources
            "/img", // Static resources
            "/favicon.ico", // Favicon
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath))
            || context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase);
    }
}
