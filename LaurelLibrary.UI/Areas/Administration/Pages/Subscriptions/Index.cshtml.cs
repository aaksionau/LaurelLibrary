using System.Security.Claims;
using System.Text.Json;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILibrariesService _librariesService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ISubscriptionService subscriptionService,
        ILibrariesService librariesService,
        ILogger<IndexModel> logger
    )
    {
        _subscriptionService = subscriptionService;
        _librariesService = librariesService;
        _logger = logger;
    }

    public SubscriptionDto? CurrentSubscription { get; set; }
    public List<SubscriptionPlanDto> AvailablePlans { get; set; } = new();
    public SubscriptionUsageDto Usage { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? message)
    {
        // Handle upgrade requirement message
        if (!string.IsNullOrEmpty(message))
        {
            TempData["ErrorMessage"] = message;
        }

        var libraryId = await GetCurrentLibraryIdAsync();
        if (!libraryId.HasValue)
        {
            // If user doesn't have a library, redirect them to create one first
            TempData["InfoMessage"] =
                "Please create or select a library before managing subscriptions.";
            return RedirectToPage("/Libraries/List", new { area = "Administration" });
        }

        CurrentSubscription = await _subscriptionService.GetLibrarySubscriptionAsync(
            libraryId.Value
        );

        // If no subscription exists, create a free one
        if (CurrentSubscription == null)
        {
            try
            {
                CurrentSubscription = await _subscriptionService.CreateFreeSubscriptionAsync(
                    libraryId.Value
                );
                TempData["SuccessMessage"] =
                    "Welcome! You've been automatically enrolled in our free Bookworm Basic plan.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating free subscription for library {LibraryId}",
                    libraryId.Value
                );
                TempData["ErrorMessage"] =
                    "There was an error setting up your subscription. Please try again.";
            }
        }

        AvailablePlans = await _subscriptionService.GetAvailablePlansAsync(libraryId.Value);
        Usage = await _subscriptionService.GetSubscriptionUsageAsync(libraryId.Value);

        return Page();
    }

    public async Task<IActionResult> OnPostSubscribeAsync([FromBody] JsonElement requestBody)
    {
        try
        {
            _logger.LogInformation(
                "Subscribe request received. User: {User}",
                User?.Identity?.Name
            );
            _logger.LogInformation("Request body: {RequestBody}", requestBody.ToString());

            var libraryId = await GetCurrentLibraryIdAsync();
            if (!libraryId.HasValue)
            {
                _logger.LogWarning("No library found for user {User}", User?.Identity?.Name);
                return BadRequest("No library selected");
            }

            // Manually parse the JSON to avoid model binding issues
            if (
                !requestBody.TryGetProperty("tier", out var tierElement)
                || !requestBody.TryGetProperty("billingInterval", out var intervalElement)
            )
            {
                _logger.LogWarning(
                    "Subscribe request missing required fields for user {User}. Request: {RequestBody}",
                    User?.Identity?.Name,
                    requestBody.ToString()
                );
                return new JsonResult(
                    new
                    {
                        success = false,
                        message = "Missing required fields: tier and billingInterval are required.",
                    }
                );
            }

            var tierString = tierElement.GetString();
            var billingInterval = intervalElement.GetString();

            if (string.IsNullOrEmpty(tierString) || string.IsNullOrEmpty(billingInterval))
            {
                return new JsonResult(
                    new { success = false, message = "Tier and billing interval cannot be empty." }
                );
            }

            // Parse the tier enum
            if (!Enum.TryParse<SubscriptionTier>(tierString, true, out var tier))
            {
                _logger.LogWarning(
                    "Invalid subscription tier '{Tier}' for user {User}",
                    tierString,
                    User?.Identity?.Name
                );
                return new JsonResult(
                    new { success = false, message = $"Invalid subscription tier: {tierString}" }
                );
            }

            // Validate billing interval
            if (billingInterval != "month" && billingInterval != "year")
            {
                return new JsonResult(
                    new { success = false, message = "Billing interval must be 'month' or 'year'." }
                );
            }

            // Create the request object manually
            var request = new CreateSubscriptionDto
            {
                LibraryId = libraryId.Value,
                Tier = tier,
                BillingInterval = billingInterval,
            };

            // Log the parsed request for debugging
            _logger.LogInformation(
                "Parsed subscription request: Tier={Tier}, BillingInterval={BillingInterval}, User={User}",
                request.Tier,
                request.BillingInterval,
                User?.Identity?.Name
            );

            // For free tier, create subscription directly
            if (request.Tier == SubscriptionTier.BookwormBasic)
            {
                _logger.LogInformation(
                    "Creating free subscription for library {LibraryId}",
                    libraryId.Value
                );
                var subscription = await _subscriptionService.CreateFreeSubscriptionAsync(
                    libraryId.Value
                );
                _logger.LogInformation(
                    "Free subscription created successfully: {SubscriptionId}",
                    subscription?.SubscriptionId
                );
                return new JsonResult(new { success = true, redirectUrl = Url.Page("Index") });
            }

            // For paid tiers, create Stripe checkout session
            var checkoutUrl = await _subscriptionService.CreateCheckoutSessionAsync(request);
            return new JsonResult(new { success = true, redirectUrl = checkoutUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        try
        {
            var libraryId = await GetCurrentLibraryIdAsync();
            if (!libraryId.HasValue)
            {
                return BadRequest("No library selected");
            }

            var subscription = await _subscriptionService.GetLibrarySubscriptionAsync(
                libraryId.Value
            );
            if (subscription == null)
            {
                return NotFound("No active subscription found");
            }

            var success = await _subscriptionService.CancelSubscriptionAsync(
                subscription.SubscriptionId
            );
            if (success)
            {
                TempData["SuccessMessage"] = "Your subscription has been cancelled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel subscription. Please contact support.";
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            TempData["ErrorMessage"] = "An error occurred while cancelling your subscription.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetUsageAsync()
    {
        var libraryId = await GetCurrentLibraryIdAsync();
        if (!libraryId.HasValue)
        {
            return BadRequest("No library selected");
        }

        var usage = await _subscriptionService.GetSubscriptionUsageAsync(libraryId.Value);
        return new JsonResult(usage);
    }

    private async Task<Guid?> GetCurrentLibraryIdAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var libraries = await _librariesService.GetLibrariesForUserAsync(userId);
            return libraries.FirstOrDefault()?.LibraryId != null
                ? Guid.Parse(libraries.FirstOrDefault()!.LibraryId!)
                : null;
        }

        return null;
    }
}
