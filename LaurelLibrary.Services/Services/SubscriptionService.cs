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
        var currentTier = currentSubscription?.Tier ?? SubscriptionTier.LibraryLover;

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
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.LibraryLover);

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
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.LibraryLover);

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
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.LibraryLover);
        return plan.SemanticSearchEnabled;
    }

    public async Task<bool> IsAgeClassificationEnabledAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        var plan = SubscriptionPlan.GetPlan(subscription?.Tier ?? SubscriptionTier.LibraryLover);
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
                var currentTier = subscription?.Tier ?? SubscriptionTier.LibraryLover;
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

    public async Task<SubscriptionDto> CreateTrialSubscriptionAsync(
        Guid libraryId,
        SubscriptionTier tier = SubscriptionTier.LibraryLover,
        string billingInterval = "month"
    )
    {
        var plan = SubscriptionPlan.GetPlan(tier);
        var amount = billingInterval == "year" ? plan.YearlyPrice : plan.MonthlyPrice;

        var existingSubscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
        if (existingSubscription != null)
        {
            existingSubscription.Status = SubscriptionStatus.Trial;
            existingSubscription.Tier = tier;
            existingSubscription.StartDate = DateTime.UtcNow;
            existingSubscription.TrialEndDate = DateTime.UtcNow.AddMonths(1); // 1 month free trial
            existingSubscription.NextBillingDate = DateTime.UtcNow.AddMonths(1);
            existingSubscription.Amount = amount; // Will be charged after trial
            existingSubscription.Currency = "USD";
            existingSubscription.BillingInterval = billingInterval;

            await _subscriptionRepository.UpdateAsync(existingSubscription);

            return MapToDto(existingSubscription);
        }

        var subscription = new Domain.Entities.Subscription
        {
            LibraryId = libraryId,
            Tier = tier,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow,
            TrialEndDate = DateTime.UtcNow.AddMonths(1), // 1 month free trial
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            Amount = amount, // Will be charged after trial
            Currency = "USD",
            BillingInterval = billingInterval,
        };

        await _subscriptionRepository.CreateAsync(subscription);
        return MapToDto(subscription);
    }

    public async Task<SubscriptionDto?> VerifyAndProcessCheckoutSessionAsync(
        string sessionId,
        Guid libraryId
    )
    {
        try
        {
            _logger.LogInformation(
                "Verifying checkout session {SessionId} for library {LibraryId}",
                sessionId,
                libraryId
            );

            // First check if subscription already exists
            var existingSubscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);
            if (
                existingSubscription != null
                && existingSubscription.Status == SubscriptionStatus.Active
            )
            {
                _logger.LogInformation(
                    "Subscription already exists for library {LibraryId}",
                    libraryId
                );
                return MapToDto(existingSubscription);
            }

            // Get session from Stripe
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);

            if (session == null)
            {
                _logger.LogWarning("Checkout session {SessionId} not found", sessionId);
                return null;
            }

            if (session.PaymentStatus != "paid")
            {
                _logger.LogWarning(
                    "Checkout session {SessionId} payment status is {PaymentStatus}",
                    sessionId,
                    session.PaymentStatus
                );
                return null;
            }

            // Verify session metadata matches the library
            if (
                session.Metadata?.ContainsKey("LibraryId") != true
                || !Guid.TryParse(session.Metadata["LibraryId"], out var sessionLibraryId)
                || sessionLibraryId != libraryId
            )
            {
                _logger.LogWarning("Checkout session {SessionId} library mismatch", sessionId);
                return null;
            }

            // If webhook hasn't processed yet, process manually
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
            _logger.LogInformation(
                "Created subscription {SubscriptionId} for library {LibraryId} from checkout session {SessionId}",
                subscription.SubscriptionId,
                libraryId,
                sessionId
            );

            return MapToDto(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error verifying and processing checkout session {SessionId} for library {LibraryId}",
                sessionId,
                libraryId
            );
            return null;
        }
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
            SubscriptionTier.LibraryLover => "Library Lover",
            SubscriptionTier.BibliothecaPro => "Bibliotheca Pro",
            _ => "Unknown",
        };
    }

    public async Task<bool> IsTrialExpiredAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);

        if (subscription == null || subscription.Status != SubscriptionStatus.Trial)
        {
            return false;
        }

        return subscription.TrialEndDate.HasValue
            && subscription.TrialEndDate.Value <= DateTime.UtcNow;
    }

    public async Task<string> GetTrialStatusMessageAsync(Guid libraryId)
    {
        var subscription = await _subscriptionRepository.GetByLibraryIdAsync(libraryId);

        if (subscription == null || subscription.Status != SubscriptionStatus.Trial)
        {
            return "";
        }

        if (!subscription.TrialEndDate.HasValue)
        {
            return "";
        }

        var daysRemaining = (subscription.TrialEndDate.Value - DateTime.UtcNow).Days;

        if (daysRemaining <= 0)
        {
            return "Your free trial has expired. Please add a payment method to continue using your subscription.";
        }
        else if (daysRemaining <= 3)
        {
            return $"Your free trial expires in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}. Add a payment method to avoid interruption.";
        }
        else if (daysRemaining <= 7)
        {
            return $"Your free trial expires in {daysRemaining} days.";
        }

        return "";
    }

    public async Task<bool> HasValidSubscriptionAsync(string userId)
    {
        try
        {
            var userLibraries = await _librariesRepository.GetLibrariesForUserAsync(userId);
            if (!userLibraries.Any())
            {
                // No libraries yet - consider valid to allow library creation
                return true;
            }

            // Check subscription for the first library (primary library)
            var primaryLibrary = userLibraries.First();
            var subscription = await _subscriptionRepository.GetByLibraryIdAsync(
                primaryLibrary.LibraryId
            );

            // Check if user has active or trial subscription
            var hasValidSubscription =
                subscription != null
                && (
                    subscription.Status == SubscriptionStatus.Active
                    || subscription.Status == SubscriptionStatus.Trial
                );

            // If trial, check if it's expired
            if (subscription?.Status == SubscriptionStatus.Trial)
            {
                var isTrialExpired =
                    subscription.TrialEndDate.HasValue
                    && subscription.TrialEndDate.Value <= DateTime.UtcNow;
                if (isTrialExpired)
                {
                    hasValidSubscription = false;
                }
            }

            return hasValidSubscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription status for user {UserId}", userId);
            return true; // Return true on error to avoid blocking users
        }
    }

    public async Task ProcessExpiredTrialsAsync()
    {
        try
        {
            _logger.LogInformation("Processing expired trials...");

            var expiredTrials = await _subscriptionRepository.GetExpiredTrialsAsync();

            foreach (var subscription in expiredTrials)
            {
                _logger.LogInformation(
                    "Processing expired trial for library {LibraryId}",
                    subscription.LibraryId
                );

                // Update status to indicate trial has expired
                subscription.Status = SubscriptionStatus.Cancelled;
                await _subscriptionRepository.UpdateAsync(subscription);

                // You might want to send an email notification here
                _logger.LogInformation(
                    "Trial expired for library {LibraryId}, status updated to Cancelled",
                    subscription.LibraryId
                );
            }

            _logger.LogInformation("Processed {Count} expired trials", expiredTrials.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired trials");
            throw;
        }
    }
}
