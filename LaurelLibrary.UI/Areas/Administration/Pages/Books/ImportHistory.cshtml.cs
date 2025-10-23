using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    public class ImportHistoryModel : PageModel
    {
        private readonly IBookImportService _bookImportService;
        private readonly ILogger<ImportHistoryModel> _logger;

        public ImportHistoryModel(
            IBookImportService bookImportService,
            ILogger<ImportHistoryModel> logger
        )
        {
            _bookImportService = bookImportService;
            _logger = logger;
        }

        public List<ImportHistory> ImportHistories { get; set; } = new List<ImportHistory>();

        public async Task OnGetAsync()
        {
            try
            {
                ImportHistories = await _bookImportService.GetImportHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading import history");
                ImportHistories = new List<ImportHistory>();
            }
        }
    }
}
