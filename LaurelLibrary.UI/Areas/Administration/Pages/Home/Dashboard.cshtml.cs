using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Home
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IReaderKioskService readerKioskService;
        private readonly IUserService userService;
        private readonly IAuthenticationService authenticationService;
        private readonly IDashboardService dashboardService;
        private readonly IOnboardingService onboardingService;

        public DashboardModel(
            IReaderKioskService readerKioskService,
            IUserService userService,
            IAuthenticationService authenticationService,
            IDashboardService dashboardService,
            IOnboardingService onboardingService
        )
        {
            this.readerKioskService = readerKioskService;
            this.userService = userService;
            this.authenticationService = authenticationService;
            this.dashboardService = dashboardService;
            this.onboardingService = onboardingService;
        }

        public List<BookInstance> BorrowedBooks { get; set; } = new List<BookInstance>();
        public DashboardStatisticsDto Statistics { get; set; } = new DashboardStatisticsDto();
        public OnboardingStatusDto OnboardingStatus { get; set; } = new OnboardingStatusDto();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await authenticationService.GetAppUserAsync();

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Check onboarding status
            OnboardingStatus = await onboardingService.GetOnboardingStatusAsync(user.Id);

            // If user hasn't completed onboarding and didn't explicitly skip, redirect to onboarding page
            // Allow skipping with ?skip=true query parameter for testing/admin purposes
            if (!OnboardingStatus.IsCompleted && !Request.Query.ContainsKey("skip"))
            {
                return RedirectToPage("./Onboarding");
            }

            if (user.CurrentLibraryId == null)
            {
                return Page();
            }

            BorrowedBooks = await readerKioskService.GetBorrowedBooksByLibraryAsync(
                user.CurrentLibraryId.Value
            );

            Statistics = await dashboardService.GetDashboardStatisticsAsync(
                user.CurrentLibraryId.Value
            );

            return Page();
        }
    }
}
