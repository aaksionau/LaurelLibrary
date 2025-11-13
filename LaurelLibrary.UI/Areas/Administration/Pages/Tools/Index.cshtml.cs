using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Tools
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public void OnGet() { }
    }
}
