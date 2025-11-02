using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByLibraryIdAsync(Guid libraryId)
    {
        return await _context
            .Subscriptions.Include(s => s.Library)
            .FirstOrDefaultAsync(s => s.LibraryId == libraryId);
    }

    public async Task<Subscription?> GetByIdAsync(Guid subscriptionId)
    {
        return await _context
            .Subscriptions.Include(s => s.Library)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        return await _context
            .Subscriptions.Include(s => s.Library)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        subscription.SubscriptionId = Guid.NewGuid();
        subscription.CreatedAt = DateTimeOffset.UtcNow;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<Subscription> UpdateAsync(Subscription subscription)
    {
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task DeleteAsync(Guid subscriptionId)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription != null)
        {
            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate)
    {
        return await _context
            .Subscriptions.Include(s => s.Library)
            .Where(s =>
                s.EndDate.HasValue
                && s.EndDate.Value <= beforeDate
                && s.Status == SubscriptionStatus.Active
            )
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetSubscriptionsByStatusAsync(SubscriptionStatus status)
    {
        return await _context
            .Subscriptions.Include(s => s.Library)
            .Where(s => s.Status == status)
            .ToListAsync();
    }
}
