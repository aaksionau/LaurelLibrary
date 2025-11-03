using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IOnboardingService
{
    /// <summary>
    /// Gets the onboarding status for a specific user
    /// </summary>
    Task<OnboardingStatusDto> GetOnboardingStatusAsync(string userId);
}
