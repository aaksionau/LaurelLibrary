using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

public class SuccessModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILibrariesService _librariesService;
    private readonly ILogger<SuccessModel> _logger;

    public SuccessModel(
        ISubscriptionService subscriptionService,
        ILibrariesService librariesService,
        ILogger<SuccessModel> logger
    )
    {
        _subscriptionService = subscriptionService;
        _librariesService = librariesService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet(string session_id)
    {
        if (string.IsNullOrEmpty(session_id))
        {
            return BadRequest("Invalid session");
        }

        try
        {
            _logger.LogInformation(
                "Processing successful payment for session {SessionId}",
                session_id
            );

            // Get the current library ID
            var libraryId = await GetCurrentLibraryIdAsync();
            if (!libraryId.HasValue)
            {
                _logger.LogWarning("No library found for user during payment success");
                TempData["ErrorMessage"] = "Unable to process subscription - no library found.";
                return RedirectToPage("Index");
            }

            // Wait a bit for webhook processing, then verify and process if needed
            await Task.Delay(2000);

            var subscription = await _subscriptionService.VerifyAndProcessCheckoutSessionAsync(
                session_id,
                libraryId.Value
            );

            if (subscription != null)
            {
                TempData["SuccessMessage"] =
                    $"Your subscription has been activated successfully! Welcome to {subscription.PlanName}.";
                _logger.LogInformation(
                    "Subscription verified for library {LibraryId}: {SubscriptionId}",
                    libraryId.Value,
                    subscription.SubscriptionId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Could not verify subscription for session {SessionId}",
                    session_id
                );
                TempData["WarningMessage"] =
                    "Payment received but subscription activation is still processing. Please refresh in a few moments.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing payment success for session {SessionId}",
                session_id
            );
            TempData["ErrorMessage"] =
                "There was an error processing your subscription. Please contact support.";
        }

        return RedirectToPage("Index");
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
