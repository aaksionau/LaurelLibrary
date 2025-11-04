using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

public class SuccessModel : PageModel
{
    public IActionResult OnGet(string session_id)
    {
        if (string.IsNullOrEmpty(session_id))
        {
            return BadRequest("Invalid session");
        }

        TempData["SuccessMessage"] =
            "Your subscription has been activated successfully! Welcome to your new plan.";
        return RedirectToPage("Index");
    }
}
