using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

[AllowAnonymous]
public class StripeWebhookModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<StripeWebhookModel> _logger;

    public StripeWebhookModel(
        ISubscriptionService subscriptionService,
        ILogger<StripeWebhookModel> logger
    )
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            _logger.LogInformation("Received Stripe webhook request");

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            _logger.LogInformation("Webhook payload length: {Length}", json.Length);
            _logger.LogInformation(
                "Webhook signature present: {HasSignature}",
                !string.IsNullOrEmpty(signature)
            );

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing Stripe signature in webhook request");
                return BadRequest("Missing Stripe signature");
            }

            _logger.LogInformation("Processing Stripe webhook...");
            await _subscriptionService.ProcessStripeWebhookAsync(json, signature);

            _logger.LogInformation("Stripe webhook processed successfully");
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook: {Message}", ex.Message);
            return BadRequest($"Webhook processing failed: {ex.Message}");
        }
    }
}
