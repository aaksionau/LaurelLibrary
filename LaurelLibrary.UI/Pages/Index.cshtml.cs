using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // If user is authenticated, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Home/Dashboard", new { area = "Administration" });
            }

            return Page();
        }
    }
}
