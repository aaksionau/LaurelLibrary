namespace LaurelLibrary.Domain.Enums;

public enum SubscriptionTier
{
    BookwormBasic = 0, // Free tier
    LibraryLover = 1, // Mid tier
    BibliothecaPro = 2, // Premium tier
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
