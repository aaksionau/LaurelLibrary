using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace LaurelLibrary.Services.Services;

public class StripeService : IStripeService
{
    private readonly ILogger<StripeService> _logger;
    private readonly string _stripeSecretKey;
    private readonly string _webhookSecret;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _logger = logger;
        _stripeSecretKey =
            configuration["Stripe:SecretKey"]
            ?? throw new ArgumentNullException("Stripe:SecretKey");
        _webhookSecret =
            configuration["Stripe:WebhookSecret"]
            ?? throw new ArgumentNullException("Stripe:WebhookSecret");

        StripeConfiguration.ApiKey = _stripeSecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string priceId,
        Guid libraryId,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null
    )
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = priceId, Quantity = 1 },
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = metadata ?? new Dictionary<string, string>(),
                AllowPromotionCodes = true,
                BillingAddressCollection = "required",
                CustomerCreation = "always",
            };

            // Add library ID to metadata
            options.Metadata["LibraryId"] = libraryId.ToString();

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating Stripe checkout session for library {LibraryId}",
                libraryId
            );
            throw;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(string stripeSubscriptionId)
    {
        try
        {
            var service = new Stripe.SubscriptionService();
            await service.CancelAsync(
                stripeSubscriptionId,
                new SubscriptionCancelOptions { InvoiceNow = true, Prorate = true }
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cancelling Stripe subscription {SubscriptionId}",
                stripeSubscriptionId
            );
            return false;
        }
    }

    public async Task<object> GetSubscriptionAsync(string stripeSubscriptionId)
    {
        try
        {
            var service = new Stripe.SubscriptionService();
            return await service.GetAsync(stripeSubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Stripe subscription {SubscriptionId}",
                stripeSubscriptionId
            );
            throw;
        }
    }

    public async Task<string> CreateCustomerAsync(
        string email,
        string name,
        Dictionary<string, string>? metadata = null
    )
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = metadata ?? new Dictionary<string, string>(),
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer for {Email}", email);
            throw;
        }
    }
}
