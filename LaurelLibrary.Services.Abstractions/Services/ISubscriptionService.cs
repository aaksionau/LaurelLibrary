using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetLibrarySubscriptionAsync(Guid libraryId);
    Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync(Guid libraryId);
    Task<string> CreateCheckoutSessionAsync(CreateSubscriptionDto request);
    Task<SubscriptionDto> UpdateSubscriptionAsync(
        Guid subscriptionId,
        SubscriptionTier newTier,
        string billingInterval
    );
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId);
    Task<SubscriptionUsageDto> GetSubscriptionUsageAsync(Guid libraryId);
    Task<bool> CanAddBookAsync(Guid libraryId);
    Task<bool> CanAddReaderAsync(Guid libraryId);
    Task<bool> CanAddLibraryAsync(string userId);
    Task ValidateBookImportLimitsAsync(Guid libraryId, int booksToImport);
    Task<bool> IsSemanticSearchEnabledAsync(Guid libraryId);
    Task<bool> IsAgeClassificationEnabledAsync(Guid libraryId);
    Task ProcessStripeWebhookAsync(string payload, string signature);
    Task<SubscriptionDto> CreateFreeSubscriptionAsync(Guid libraryId);
    Task<SubscriptionDto?> VerifyAndProcessCheckoutSessionAsync(string sessionId, Guid libraryId);
}

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        string priceId,
        Guid libraryId,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null
    );
    Task<bool> CancelSubscriptionAsync(string stripeSubscriptionId);
    Task<object> GetSubscriptionAsync(string stripeSubscriptionId);
    Task<string> CreateCustomerAsync(
        string email,
        string name,
        Dictionary<string, string>? metadata = null
    );
}
