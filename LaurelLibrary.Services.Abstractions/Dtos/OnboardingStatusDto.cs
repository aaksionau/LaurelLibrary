namespace LaurelLibrary.Services.Abstractions.Dtos;

public class OnboardingStatusDto
{
    public bool HasLibrary { get; set; }
    public bool HasKiosk { get; set; }
    public bool HasReader { get; set; }
    public bool HasBook { get; set; }
    public Guid? LibraryId { get; set; }
    public bool IsCompleted => HasLibrary && HasKiosk && HasReader && HasBook;
    public int CompletedSteps =>
        (HasLibrary ? 1 : 0) + (HasKiosk ? 1 : 0) + (HasReader ? 1 : 0) + (HasBook ? 1 : 0);
    public int TotalSteps => 4;
}
