using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Domain.Exceptions;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace LaurelLibrary.Services.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IBooksRepository _booksRepository;
    private readonly IReadersRepository _readersRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _webhookSecret;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IBooksRepository booksRepository,
        IReadersRepository readersRepository,
        ILibrariesRepository librariesRepository,
        IStripeService stripeService,
        ILogger<SubscriptionService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration
    )
    {
        _subscriptionRepository = subscriptionRepository;
        _booksRepository = booksRepository;
        _readersRepository = readersRepository;
        _librariesRepository = librariesRepository;
        _stripeService = stripeService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _webhookSecret =
            configuration["Stripe:WebhookSecret"]
            ?? throw new ArgumentNullException("Stripe:WebhookSecret");
    }

    public async Task<SubscriptionDto?> GetLibrarySubscriptionAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        if (subscription == null)
        {
            return null;
        }

        return MapToDto(subscription);
    }

    public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync(Guid libraryId)
    {
        var currentSubscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        var currentTier = currentSubscription?.Tier ?? SubscriptionTier.BookwormBasic;

        return SubscriptionPlan
            .Plans.Select(plan => new SubscriptionPlanDto
            {
                Tier = plan.Tier,
                Name = plan.Name,
                Description = plan.Description,
                MonthlyPrice = plan.MonthlyPrice,
                YearlyPrice = plan.YearlyPrice,
                MaxBooks = plan.MaxBooks,
                MaxReaders = plan.MaxReaders,
                MaxLibraries = plan.MaxLibraries,
                SemanticSearchEnabled = plan.SemanticSearchEnabled,
                AgeClassificationEnabled = plan.AgeClassificationEnabled,
                PrioritySupport = plan.PrioritySupport,
                Features = plan.Features,
                IsCurrentPlan = plan.Tier == currentTier,
            })
            .ToList();
    }

    public async Task<string> CreateCheckoutSessionAsync(CreateSubscriptionDto request)
    {
        var plan = SubscriptionPlan.GetPlan(request.Tier);
        var priceId =
            request.BillingInterval == "year"
                ? plan.StripeYearlyPriceId
                : plan.StripeMonthlyPriceId;

        if (string.IsNullOrEmpty(priceId))
        {
            throw new InvalidOperationException("Price ID not configured for this plan");
        }

        var baseUrl = GetBaseUrl();
        var successUrl =
            request.SuccessUrl
            ?? $"{baseUrl}/administration/subscriptions/success?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl =
            request.CancelUrl ?? $"{baseUrl}/administration/subscriptions/cancelcheckout";

        var metadata = new Dictionary<string, string>
        {
            ["LibraryId"] = request.LibraryId.ToString(),
            ["Tier"] = request.Tier.ToString(),
            ["BillingInterval"] = request.BillingInterval,
        };

        return await _stripeService.CreateCheckoutSessionAsync(
            priceId,
            request.LibraryId,
            successUrl,
            cancelUrl,
            metadata
        );
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(
        Guid subscriptionId,
        SubscriptionTier newTier,
        string billingInterval
    )
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null)
        {
            throw new ArgumentException("Subscription not found");
        }

        var plan = SubscriptionPlan.GetPlan(newTier);
        subscription.Tier = newTier;
        subscription.Amount = billingInterval == "year" ? plan.YearlyPrice : plan.MonthlyPrice;
        subscription.BillingInterval = billingInterval;

        await _subscriptionRepository.UpdateAsync(subscription);
        return MapToDto(subscription);
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            return false;
        }

        var success = await _stripeService.CancelSubscriptionAsync(
            subscription.StripeSubscriptionId
        );
        if (success)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.EndDate = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(subscription);
        }

        return success;
    }

    public async Task<SubscriptionUsageDto> GetSubscriptionUsageAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.BookwormBasic);

        // Get current counts
        var currentBooks = await GetBookCountForLibraryAsync(libraryId);
        var currentReaders = await GetReaderCountForLibraryAsync(libraryId);
        var currentLibraries = await GetLibraryCountForUserAsync(libraryId);

        return new SubscriptionUsageDto
        {
            CurrentBooks = currentBooks,
            MaxBooks = plan.MaxBooks,
            CurrentReaders = currentReaders,
            MaxReaders = plan.MaxReaders,
            CurrentLibraries = currentLibraries,
            MaxLibraries = plan.MaxLibraries,
            IsSemanticSearchEnabled = plan.SemanticSearchEnabled,
            IsAgeClassificationEnabled = plan.AgeClassificationEnabled,
        };
    }

    public async Task<bool> CanAddBookAsync(Guid libraryId)
    {
        var usage = await GetSubscriptionUsageAsync(libraryId);
        return usage.MaxBooks == -1 || usage.CurrentBooks < usage.MaxBooks;
    }

    public async Task<bool> CanAddReaderAsync(Guid libraryId)
    {
        var usage = await GetSubscriptionUsageAsync(libraryId);
        return usage.MaxReaders == -1 || usage.CurrentReaders < usage.MaxReaders;
    }

    public async Task<bool> CanAddLibraryAsync(string userId)
    {
        // Get the user's subscription from any of their existing libraries
        var userLibraries = await _librariesRepository.GetLibrariesForUserAsync(userId);

        if (userLibraries.Count == 0)
        {
            // User has no libraries, they can create their first one
            return true;
        }

        // Get the subscription from the user's first library to determine their tier
        var firstLibraryId = userLibraries.First().LibraryId;
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(firstLibraryId);
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.BookwormBasic);

        // Check if unlimited libraries are allowed
        if (plan.MaxLibraries == -1)
        {
            return true; // Unlimited libraries
        }

        // Check if current library count is below the limit
        return userLibraries.Count < plan.MaxLibraries;
    }

    public async Task<bool> IsSemanticSearchEnabledAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.BookwormBasic);
        return plan.SemanticSearchEnabled;
    }

    public async Task<bool> IsAgeClassificationEnabledAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.BookwormBasic);
        return plan.AgeClassificationEnabled;
    }

    public async Task ValidateBookImportLimitsAsync(Guid libraryId, int booksToImport)
    {
        // Get current subscription usage
        var usage = await GetSubscriptionUsageAsync(libraryId);

        // Check if subscription allows adding books
        if (usage.MaxBooks != -1) // -1 means unlimited
        {
            var availableSlots = usage.MaxBooks - usage.CurrentBooks;

            if (booksToImport > availableSlots)
            {
                var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
                var currentTier = subscription?.Tier ?? SubscriptionTier.BookwormBasic;
                var subscriptionName = GetSubscriptionTierDisplayName(currentTier);

                _logger.LogWarning(
                    "Book import validation failed for library {LibraryId}. Attempted to import {BooksToImport} books but only {AvailableSlots} slots available. "
                        + "Current: {CurrentBooks}, Max: {MaxBooks}, Tier: {SubscriptionTier}",
                    libraryId,
                    booksToImport,
                    availableSlots,
                    usage.CurrentBooks,
                    usage.MaxBooks,
                    currentTier
                );

                throw new SubscriptionUpgradeRequiredException(
                    $"Cannot import {booksToImport} books. Your {subscriptionName} subscription allows a maximum of {usage.MaxBooks} books. You currently have {usage.CurrentBooks} books, leaving {availableSlots} available slots.",
                    "Book Import",
                    subscriptionName,
                    "higher tier"
                );
            }
        }

        _logger.LogInformation(
            "Book import validation passed for library {LibraryId}. Importing {BooksToImport} books. "
                + "Current books: {CurrentBooks}, Max books: {MaxBooks}",
            libraryId,
            booksToImport,
            usage.CurrentBooks,
            usage.MaxBooks == -1 ? "unlimited" : usage.MaxBooks.ToString()
        );
    }

    public async Task ProcessStripeWebhookAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);

            switch (stripeEvent.Type)
            {
                case Events.CheckoutSessionCompleted:
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;
                case Events.InvoicePaymentSucceeded:
                    await HandleInvoicePaymentSucceeded(stripeEvent);
                    break;
                case Events.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;
                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                default:
                    _logger.LogInformation(
                        "Unhandled Stripe webhook event: {EventType}",
                        stripeEvent.Type
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            throw;
        }
    }

    public async Task<SubscriptionDto> CreateFreeSubscriptionAsync(Guid libraryId)
    {
        var existingSubscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        if (existingSubscription != null)
        {
            return MapToDto(existingSubscription);
        }

        var subscription = new Domain.Entities.Subscription
        {
            LibraryId = libraryId,
            Tier = SubscriptionTier.BookwormBasic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddYears(100), // Never bills
            Amount = 0,
            Currency = "USD",
            BillingInterval = "month",
        };

        await _subscriptionRepository.CreateAsync(subscription);
        return MapToDto(subscription);
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.Metadata?.ContainsKey("LibraryId") == true)
        {
            var libraryId = Guid.Parse(session.Metadata["LibraryId"]);
            var tier = Enum.Parse<SubscriptionTier>(session.Metadata["Tier"]);
            var billingInterval = session.Metadata["BillingInterval"];

            var plan = SubscriptionPlan.GetPlan(tier);
            var amount = billingInterval == "year" ? plan.YearlyPrice : plan.MonthlyPrice;

            var subscription = new Domain.Entities.Subscription
            {
                LibraryId = libraryId,
                Tier = tier,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                NextBillingDate = DateTime.UtcNow.AddMonths(billingInterval == "year" ? 12 : 1),
                Amount = amount,
                Currency = "USD",
                BillingInterval = billingInterval,
                StripeSubscriptionId = session.SubscriptionId,
                StripeCustomerId = session.CustomerId,
            };

            await _subscriptionRepository.CreateAsync(subscription);
        }
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (!string.IsNullOrEmpty(invoice?.SubscriptionId))
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
                invoice.SubscriptionId
            );
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.NextBillingDate = DateTime.UtcNow.AddMonths(
                    subscription.BillingInterval == "year" ? 12 : 1
                );
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (!string.IsNullOrEmpty(invoice?.SubscriptionId))
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
                invoice.SubscriptionId
            );
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.PastDue;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription != null)
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
                stripeSubscription.Id
            );
            if (subscription != null)
            {
                subscription.Status = stripeSubscription.Status switch
                {
                    "active" => SubscriptionStatus.Active,
                    "past_due" => SubscriptionStatus.PastDue,
                    "canceled" => SubscriptionStatus.Cancelled,
                    "unpaid" => SubscriptionStatus.Unpaid,
                    "incomplete" => SubscriptionStatus.Incomplete,
                    "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
                    _ => subscription.Status,
                };

                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription != null)
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
                stripeSubscription.Id
            );
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.EndDate = DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }
    }

    private SubscriptionDto MapToDto(Domain.Entities.Subscription subscription)
    {
        var plan = SubscriptionPlan.GetPlan(subscription.Tier);

        return new SubscriptionDto
        {
            SubscriptionId = subscription.SubscriptionId,
            LibraryId = subscription.LibraryId,
            Tier = subscription.Tier,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            NextBillingDate = subscription.NextBillingDate,
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            BillingInterval = subscription.BillingInterval,
            PlanName = plan.Name,
            PlanDescription = plan.Description,
            MaxBooks = plan.MaxBooks,
            MaxReaders = plan.MaxReaders,
            SemanticSearchEnabled = plan.SemanticSearchEnabled,
            AgeClassificationEnabled = plan.AgeClassificationEnabled,
            PrioritySupport = plan.PrioritySupport,
            Features = plan.Features,
        };
    }

    private async Task<int> GetBookCountForLibraryAsync(Guid libraryId)
    {
        return await _booksRepository.GetBookCountByLibraryIdAsync(libraryId);
    }

    private async Task<int> GetReaderCountForLibraryAsync(Guid libraryId)
    {
        return await _readersRepository.GetReaderCountByLibraryIdAsync(libraryId);
    }

    private async Task<int> GetLibraryCountForUserAsync(Guid libraryId)
    {
        // Get the library to find its administrators
        var library = await _librariesRepository.GetByIdWithDetailsAsync(libraryId);
        if (library?.Administrators?.Any() == true)
        {
            // Get the first administrator (assuming shared ownership across all libraries)
            var userId = library.Administrators.First().Id;
            return await _librariesRepository.GetLibraryCountByUserIdAsync(userId);
        }

        // If no administrators found, return 1 (just this library)
        return 1;
    }

    private string GetBaseUrl()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }
        return "https://localhost"; // Fallback
    }

    private static string GetSubscriptionTierDisplayName(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.BookwormBasic => "Bookworm Basic",
            SubscriptionTier.LibraryLover => "Library Lover",
            SubscriptionTier.BibliothecaPro => "Bibliotheca Pro",
            _ => "Unknown",
        };
    }
}
