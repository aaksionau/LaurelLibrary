using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Home
{
    [Authorize]
    public class OnboardingModel : PageModel
    {
        private readonly IAuthenticationService authenticationService;
        private readonly IOnboardingService onboardingService;

        public OnboardingModel(
            IAuthenticationService authenticationService,
            IOnboardingService onboardingService
        )
        {
            this.authenticationService = authenticationService;
            this.onboardingService = onboardingService;
        }

        public OnboardingStatusDto OnboardingStatus { get; set; } = new OnboardingStatusDto();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await authenticationService.GetAppUserAsync();

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Get onboarding status
            OnboardingStatus = await onboardingService.GetOnboardingStatusAsync(user.Id);

            // If onboarding is completed, redirect to dashboard
            if (OnboardingStatus.IsCompleted)
            {
                return RedirectToPage("./Dashboard");
            }

            return Page();
        }
    }
}
