using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Libraries
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly ILibrariesRepository librariesRepository;
        private readonly IAuthenticationService userService;

        public ListModel(
            ILibrariesRepository librariesRepository,
            IAuthenticationService userService
        )
        {
            this.librariesRepository = librariesRepository;
            this.userService = userService;
        }

        public List<LibrarySummaryDto> Libraries { get; set; } = new List<LibrarySummaryDto>();

        public async Task OnGetAsync()
        {
            var user = await this.userService.GetAppUserAsync();
            this.Libraries = await this.librariesRepository.GetAllAsync(user.Id);
        }
    }
}
