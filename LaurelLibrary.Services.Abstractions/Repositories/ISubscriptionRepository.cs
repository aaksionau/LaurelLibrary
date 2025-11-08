using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByLibraryIdAsync(Guid libraryId);
    Task<Subscription?> GetByIdAsync(Guid subscriptionId);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid subscriptionId);
    Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate);
    Task<List<Subscription>> GetSubscriptionsByStatusAsync(SubscriptionStatus status);
    Task<List<Subscription>> GetExpiredTrialsAsync();
}
