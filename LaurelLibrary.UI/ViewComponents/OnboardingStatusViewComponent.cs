using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.ViewComponents
{
    public class OnboardingStatusViewComponent : ViewComponent
    {
        private readonly IOnboardingService onboardingService;
        private readonly IAuthenticationService authenticationService;

        public OnboardingStatusViewComponent(
            IOnboardingService onboardingService,
            IAuthenticationService authenticationService
        )
        {
            this.onboardingService = onboardingService;
            this.authenticationService = authenticationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Content(string.Empty);
            }

            // Don't show onboarding status on the onboarding page itself
            var currentPage = ViewContext.RouteData.Values["page"]?.ToString();
            if (currentPage?.Contains("Onboarding", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Content(string.Empty);
            }

            var user = await authenticationService.GetAppUserAsync();
            if (user == null)
            {
                return Content(string.Empty);
            }

            var onboardingStatus = await onboardingService.GetOnboardingStatusAsync(user.Id);
            return View(onboardingStatus);
        }
    }
}
