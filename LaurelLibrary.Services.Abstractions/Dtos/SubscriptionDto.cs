using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class SubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public Guid LibraryId { get; set; }
    public SubscriptionTier Tier { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime NextBillingDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingInterval { get; set; } = "month";
    public string PlanName { get; set; } = string.Empty;
    public string PlanDescription { get; set; } = string.Empty;
    public int MaxBooks { get; set; }
    public int MaxReaders { get; set; }
    public bool SemanticSearchEnabled { get; set; }
    public bool AgeClassificationEnabled { get; set; }
    public bool PrioritySupport { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
}

public class CreateSubscriptionDto
{
    [Required]
    public Guid LibraryId { get; set; }

    [Required]
    public SubscriptionTier Tier { get; set; }

    [Required]
    public string BillingInterval { get; set; } = "month"; // month or year

    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class SubscriptionPlanDto
{
    public SubscriptionTier Tier { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxBooks { get; set; }
    public int MaxReaders { get; set; }
    public int MaxLibraries { get; set; }
    public bool SemanticSearchEnabled { get; set; }
    public bool AgeClassificationEnabled { get; set; }
    public bool PrioritySupport { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
    public bool IsCurrentPlan { get; set; }
}

public class SubscriptionUsageDto
{
    public int CurrentBooks { get; set; }
    public int MaxBooks { get; set; }
    public int CurrentReaders { get; set; }
    public int MaxReaders { get; set; }
    public int CurrentLibraries { get; set; }
    public int MaxLibraries { get; set; }
    public bool IsSemanticSearchEnabled { get; set; }
    public bool IsAgeClassificationEnabled { get; set; }
    public double BookUsagePercentage => MaxBooks > 0 ? (double)CurrentBooks / MaxBooks * 100 : 0;
    public double ReaderUsagePercentage =>
        MaxReaders > 0 ? (double)CurrentReaders / MaxReaders * 100 : 0;
    public double LibraryUsagePercentage =>
        MaxLibraries > 0 ? (double)CurrentLibraries / MaxLibraries * 100 : 0;
}
