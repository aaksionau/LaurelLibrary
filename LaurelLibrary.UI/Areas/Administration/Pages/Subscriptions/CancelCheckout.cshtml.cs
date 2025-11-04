using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

public class CancelCheckoutModel : PageModel
{
    public IActionResult OnGet()
    {
        TempData["InfoMessage"] = "Subscription creation was cancelled.";
        return RedirectToPage("Index");
    }
}
