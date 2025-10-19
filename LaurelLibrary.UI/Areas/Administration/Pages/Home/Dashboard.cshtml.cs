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

        public DashboardModel(IBooksService booksService, IUserService userService)
        {
            this.booksService = booksService;
            this.userService = userService;
        }

        public List<BookInstance> BorrowedBooks { get; set; } = new List<BookInstance>();

        public async Task OnGetAsync()
        {
            var user = await userService.GetAppUserAsync();

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
