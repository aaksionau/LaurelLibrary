using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Domain.Entities;

public class SubscriptionPlan
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
    public string StripeMonthlyPriceId { get; set; } = string.Empty;
    public string StripeYearlyPriceId { get; set; } = string.Empty;

    public static readonly SubscriptionPlan[] Plans = new[]
    {
        new SubscriptionPlan
        {
            Tier = SubscriptionTier.BookwormBasic,
            Name = "Bookworm Basic",
            Description = "Perfect for small personal libraries or getting started",
            MonthlyPrice = 0m,
            YearlyPrice = 0m,
            MaxBooks = 100,
            MaxReaders = 10,
            MaxLibraries = 1,
            SemanticSearchEnabled = false,
            AgeClassificationEnabled = false,
            PrioritySupport = false,
            Features = new[]
            {
                "Up to 100 books",
                "Up to 10 readers",
                "1 library",
                "Mobile app checkout (*)",
                "Export your data (*)",
                "Barcode generation",
                "Email notifications",
                "Community support",
            },
            StripeMonthlyPriceId = "",
            StripeYearlyPriceId = "",
        },
        new SubscriptionPlan
        {
            Tier = SubscriptionTier.LibraryLover,
            Name = "Library Lover",
            Description = "Ideal for personal libraries with advanced features",
            MonthlyPrice = 11.99m,
            YearlyPrice = 109.99m,
            MaxBooks = 1000,
            MaxReaders = 100,
            MaxLibraries = 1,
            SemanticSearchEnabled = true,
            AgeClassificationEnabled = false,
            PrioritySupport = false,
            Features = new[]
            {
                "Up to 1,000 books",
                "Up to 100 readers",
                "1 library",
                "Mobile app checkout (*)",
                "Advanced semantic search",
                "Email notifications",
                "Barcode generation",
                "Export your data (*)",
                "Standard support",
            },
            StripeMonthlyPriceId = "price_1SPatMDrPbflF5o5j3JGwPDU",
            StripeYearlyPriceId = "price_1SPauVDrPbflF5o50fgYHZWK",
        },
        new SubscriptionPlan
        {
            Tier = SubscriptionTier.BibliothecaPro,
            Name = "Bibliotheca Pro",
            Description = "Choice for small to medium libraries with premium features",
            MonthlyPrice = 14.99m,
            YearlyPrice = 149.99m,
            MaxBooks = -1, // Unlimited
            MaxReaders = -1, // Unlimited
            MaxLibraries = -1, // Unlimited
            SemanticSearchEnabled = true,
            AgeClassificationEnabled = true,
            PrioritySupport = true,
            Features = new[]
            {
                "Unlimited books",
                "Unlimited readers",
                "Unlimited libraries",
                "Advanced semantic search",
                "AI-powered age classification",
                "Email notifications",
                "Barcode generation",
                "Export your data (*)",
                "Mobile app checkout (*)",
                "Multi-library support",
                "Kiosk checkout system",
                "Priority support",
                "Advanced analytics (*)",
                "Custom integrations",
            },
            StripeMonthlyPriceId = "price_1SPawWDrPbflF5o5zvW6nr0e",
            StripeYearlyPriceId = "price_1SPaxfDrPbflF5o5UsvTdd9R",
        },
    };

    public static SubscriptionPlan GetPlan(SubscriptionTier tier)
    {
        return Plans.First(p => p.Tier == tier);
    }
}
