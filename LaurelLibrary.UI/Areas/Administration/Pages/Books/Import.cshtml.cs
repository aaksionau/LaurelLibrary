using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    public class ImportModel : PageModel
    {
        private readonly IBookImportService _bookImportService;
        private readonly ILogger<ImportModel> _logger;

        public ImportModel(IBookImportService bookImportService, ILogger<ImportModel> logger)
        {
            _bookImportService = bookImportService;
            _logger = logger;
        }

        [BindProperty]
        public IFormFile? CsvFile { get; set; }

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
        public int TotalIsbns { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (CsvFile == null || CsvFile.Length == 0)
            {
                Message = "Please select a CSV file to upload.";
                IsSuccess = false;
                return Page();
            }

            // Validate file extension
            var extension = Path.GetExtension(CsvFile.FileName).ToLowerInvariant();
            if (extension != ".csv")
            {
                Message = "Only CSV files are allowed.";
                IsSuccess = false;
                return Page();
            }

            // Validate file size (max 5MB)
            if (CsvFile.Length > 5 * 1024 * 1024)
            {
                Message = "File size must not exceed 5MB.";
                IsSuccess = false;
                return Page();
            }

            try
            {
                using var stream = CsvFile.OpenReadStream();
                var importHistory = await _bookImportService.ImportBooksFromCsvAsync(
                    stream,
                    CsvFile.FileName
                );

                TotalIsbns = importHistory.TotalIsbns;
                SuccessCount = importHistory.SuccessCount;
                FailedCount = importHistory.FailedCount;

                Message =
                    $"Import completed successfully! {SuccessCount} books imported, {FailedCount} failed out of {TotalIsbns} ISBNs.";
                IsSuccess = true;

                _logger.LogInformation(
                    "Bulk import completed: {ImportId}, {Success}/{Total} books",
                    importHistory.ImportHistoryId,
                    SuccessCount,
                    TotalIsbns
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                Message = $"An error occurred during import: {ex.Message}";
                IsSuccess = false;
            }

            return Page();
        }
    }
}
