using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Subscriptions;

[AllowAnonymous]
public class TestModel : PageModel
{
    public IActionResult OnGet()
    {
        return new JsonResult(
            new { message = "Subscription controller is working!", timestamp = DateTime.Now }
        );
    }
}
