namespace LaurelLibrary.Domain.Enums;

public enum SubscriptionTier
{
    LibraryLover = 0, // Mid tier with free trial
    BibliothecaPro = 1, // Premium tier with free trial
}

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Unpaid = 4,
    Incomplete = 5,
    IncompleteExpired = 6,
}
