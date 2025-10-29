using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Home
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IBooksService booksService;
        private readonly IUserService userService;
        private readonly IAuthenticationService authenticationService;

        public DashboardModel(
            IBooksService booksService,
            IUserService userService,
            IAuthenticationService authenticationService
        )
        {
            this.booksService = booksService;
            this.userService = userService;
            this.authenticationService = authenticationService;
        }

        public List<BookInstance> BorrowedBooks { get; set; } = new List<BookInstance>();

        public async Task OnGetAsync()
        {
            var user = await authenticationService.GetAppUserAsync();

            if (user?.CurrentLibraryId == null)
            {
                return;
            }

            BorrowedBooks = await booksService.GetBorrowedBooksByLibraryAsync(
                user.CurrentLibraryId.Value
            );
        }
    }
}
