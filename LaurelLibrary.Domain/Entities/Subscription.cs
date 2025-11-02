using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Domain.Entities;

public class Subscription : Audit
{
    public Guid SubscriptionId { get; set; }

    [Required]
    public Guid LibraryId { get; set; }

    [Required]
    public SubscriptionTier Tier { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(255)]
    public string? StripeSubscriptionId { get; set; }

    [StringLength(255)]
    public string? StripeCustomerId { get; set; }

    [StringLength(255)]
    public string? StripePriceId { get; set; }

    public DateTime? TrialEndDate { get; set; }

    public DateTime NextBillingDate { get; set; }

    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "USD";

    [StringLength(50)]
    public string BillingInterval { get; set; } = "month"; // month, year

    // Navigation properties
    public virtual Library Library { get; set; } = null!;
}
