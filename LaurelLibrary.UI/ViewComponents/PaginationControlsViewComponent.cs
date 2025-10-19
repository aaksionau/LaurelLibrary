using System.Threading.Tasks;
using LaurelLibrary.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.ViewComponents
{
    public class PaginationControlsViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync(PaginationViewModel model)
        {
            return Task.FromResult((IViewComponentResult)View(model));
        }
    }
}
