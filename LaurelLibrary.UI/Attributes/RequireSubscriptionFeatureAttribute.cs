using System.Security.Claims;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LaurelLibrary.UI.Attributes;

public class RequireSubscriptionFeatureAttribute : ActionFilterAttribute
{
    private readonly SubscriptionFeature _requiredFeature;

    public RequireSubscriptionFeatureAttribute(SubscriptionFeature requiredFeature)
    {
        _requiredFeature = requiredFeature;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var subscriptionService =
            context.HttpContext.RequestServices.GetRequiredService<ISubscriptionService>();
        var libraryId = GetLibraryIdFromContext(context.HttpContext);

        if (libraryId.HasValue)
        {
            bool hasFeature = _requiredFeature switch
            {
                SubscriptionFeature.SemanticSearch =>
                    await subscriptionService.IsSemanticSearchEnabledAsync(libraryId.Value),
                SubscriptionFeature.AgeClassification =>
                    await subscriptionService.IsAgeClassificationEnabledAsync(libraryId.Value),
                _ => true,
            };

            if (!hasFeature)
            {
                if (IsApiRequest(context.HttpContext))
                {
                    context.Result = new JsonResult(
                        new
                        {
                            success = false,
                            message = $"This feature requires a subscription plan that includes {_requiredFeature}. Please upgrade your plan.",
                            featureRequired = _requiredFeature.ToString(),
                        }
                    )
                    {
                        StatusCode = 403,
                    };
                    return;
                }
                else
                {
                    var errorMessage =
                        $"This feature requires a subscription plan that includes {_requiredFeature}. Please upgrade your plan.";
                    context.Result = new RedirectResult(
                        $"/Subscription?error={Uri.EscapeDataString(errorMessage)}"
                    );
                    return;
                }
            }
        }

        await next();
    }

    private static Guid? GetLibraryIdFromContext(HttpContext context)
    {
        // Try to get from claims first
        var libraryIdClaim = context.User.FindFirst("LibraryId");
        if (libraryIdClaim != null && Guid.TryParse(libraryIdClaim.Value, out var libraryId))
        {
            return libraryId;
        }

        // Try to get from route or query parameters
        if (
            context.Request.RouteValues.TryGetValue("libraryId", out var routeLibraryId)
            && routeLibraryId != null
            && Guid.TryParse(routeLibraryId.ToString(), out var parsedLibraryId)
        )
        {
            return parsedLibraryId;
        }

        return null;
    }

    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api")
            || context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true);
    }
}

public enum SubscriptionFeature
{
    SemanticSearch,
    AgeClassification,
}
