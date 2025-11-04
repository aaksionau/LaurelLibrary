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
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest("Missing Stripe signature");
            }

            await _subscriptionService.ProcessStripeWebhookAsync(json, signature);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest();
        }
    }
}
