namespace LaurelLibrary.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user needs to upgrade their subscription to access a feature or exceed current limits.
/// </summary>
public class SubscriptionUpgradeRequiredException : Exception
{
    /// <summary>
    /// Gets the URL where the user should be redirected to upgrade their subscription.
    /// </summary>
    public string RedirectUrl { get; }

    /// <summary>
    /// Gets the feature or limit that requires an upgrade.
    /// </summary>
    public string? Feature { get; }

    /// <summary>
    /// Gets the current tier name.
    /// </summary>
    public string? CurrentTier { get; }

    /// <summary>
    /// Gets the recommended tier name.
    /// </summary>
    public string? RecommendedTier { get; }

    public SubscriptionUpgradeRequiredException(
        string message,
        string redirectUrl = "/Administration/Subscriptions"
    )
        : base(message)
    {
        RedirectUrl = redirectUrl;
    }

    public SubscriptionUpgradeRequiredException(
        string message,
        string feature,
        string currentTier,
        string recommendedTier,
        string redirectUrl = "/Administration/Subscriptions"
    )
        : base(message)
    {
        Feature = feature;
        CurrentTier = currentTier;
        RecommendedTier = recommendedTier;
        RedirectUrl = redirectUrl;
    }

    public SubscriptionUpgradeRequiredException(
        string message,
        Exception innerException,
        string redirectUrl = "/Administration/Subscriptions"
    )
        : base(message, innerException)
    {
        RedirectUrl = redirectUrl;
    }
}
