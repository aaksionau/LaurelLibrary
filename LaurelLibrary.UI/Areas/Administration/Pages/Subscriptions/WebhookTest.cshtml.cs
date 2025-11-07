using System.Text.Json;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

[Authorize]
public class WebhookTestModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILibrariesService _librariesService;
    private readonly ILogger<WebhookTestModel> _logger;

    public WebhookTestModel(
        ISubscriptionService subscriptionService,
        ILibrariesService librariesService,
        ILogger<WebhookTestModel> logger
    )
    {
        _subscriptionService = subscriptionService;
        _librariesService = librariesService;
        _logger = logger;
    }

    public string? TestResult { get; set; }

    public void OnGet()
    {
        // Page load
    }

    public async Task<IActionResult> OnPostVerifySessionAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                TestResult = "Error: Session ID is required";
                return Page();
            }

            var libraryId = await GetCurrentLibraryIdAsync();
            if (!libraryId.HasValue)
            {
                TestResult = "Error: No library found for current user";
                return Page();
            }

            _logger.LogInformation("Testing session verification for {SessionId}", sessionId);

            // Test the verification process
            var subscription = await _subscriptionService.VerifyAndProcessCheckoutSessionAsync(
                sessionId,
                libraryId.Value
            );

            var result = new
            {
                SessionId = sessionId,
                LibraryId = libraryId.Value,
                Success = subscription != null,
                SubscriptionFound = subscription != null,
                SubscriptionDetails = subscription != null
                    ? new
                    {
                        subscription.SubscriptionId,
                        subscription.Tier,
                        subscription.Status,
                        subscription.PlanName,
                        subscription.Amount,
                        subscription.BillingInterval,
                    }
                    : null,
                Timestamp = DateTime.UtcNow,
            };

            TestResult = JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions { WriteIndented = true }
            );

            if (subscription != null)
            {
                TempData["SuccessMessage"] =
                    "Session verified and subscription processed successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Session could not be verified or processed.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing session verification for {SessionId}", sessionId);
            TestResult = $"Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }

        return Page();
    }

    private async Task<Guid?> GetCurrentLibraryIdAsync()
    {
        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var libraries = await _librariesService.GetLibrariesForUserAsync(userId);
        var firstLibrary = libraries?.FirstOrDefault();

        if (
            firstLibrary?.LibraryId != null
            && Guid.TryParse(firstLibrary.LibraryId, out var libraryId)
        )
        {
            return libraryId;
        }

        return null;
    }
}
