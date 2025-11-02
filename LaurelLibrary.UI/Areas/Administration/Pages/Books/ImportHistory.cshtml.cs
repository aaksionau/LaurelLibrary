using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
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

        // Pagination properties
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Properties for import functionality
        [BindProperty]
        public IFormFile? CsvFile { get; set; }

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
        public int TotalIsbns { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public Guid? ImportHistoryId { get; set; }

        // Add property to track current tab
        public string? CurrentTab { get; set; }

        public async Task OnGetAsync(int? pageNumber, int? pageSize, string? tab)
        {
            // Store the current tab
            CurrentTab = tab;

            try
            {
                var pagedResult = await _bookImportService.GetImportHistoryPagedAsync(
                    pageNumber ?? 1,
                    pageSize ?? 10
                );

                ImportHistories = pagedResult.Items.ToList();
                PageNumber = pagedResult.Page;
                PageSize = pagedResult.PageSize;
                TotalPages = pagedResult.TotalPages;
                TotalCount = pagedResult.TotalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading import history");
                ImportHistories = new List<ImportHistory>();
                PageNumber = pageNumber ?? 1;
                PageSize = pageSize ?? 10;
                TotalPages = 0;
                TotalCount = 0;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (CsvFile == null || CsvFile.Length == 0)
            {
                Message = "Please select a CSV file to upload.";
                IsSuccess = false;
                await LoadImportHistoryAsync();
                return Page();
            }

            // Validate file extension
            var extension = Path.GetExtension(CsvFile.FileName).ToLowerInvariant();
            if (extension != ".csv")
            {
                Message = "Only CSV files are allowed.";
                IsSuccess = false;
                await LoadImportHistoryAsync();
                return Page();
            }

            // Validate file size (max 5MB)
            if (CsvFile.Length > 5 * 1024 * 1024)
            {
                Message = "File size must not exceed 5MB.";
                IsSuccess = false;
                await LoadImportHistoryAsync();
                return Page();
            }

            try
            {
                using var stream = CsvFile.OpenReadStream();
                var importHistory = await _bookImportService.ImportBooksFromCsvAsync(
                    stream,
                    CsvFile.FileName
                );

                ImportHistoryId = importHistory.ImportHistoryId;
                TotalIsbns = importHistory.TotalIsbns;
                SuccessCount = importHistory.SuccessCount;
                FailedCount = importHistory.FailedCount;

                Message =
                    $"Import started successfully! Processing {TotalIsbns} ISBNs in the background.";
                IsSuccess = true;

                _logger.LogInformation(
                    "Bulk import started: {ImportId}, Total ISBNs: {Total}",
                    importHistory.ImportHistoryId,
                    TotalIsbns
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                Message = $"An error occurred during import: {ex.Message}";
                IsSuccess = false;
            }

            await LoadImportHistoryAsync();
            return Page();
        }

        private async Task LoadImportHistoryAsync()
        {
            try
            {
                var pagedResult = await _bookImportService.GetImportHistoryPagedAsync(
                    PageNumber > 0 ? PageNumber : 1,
                    PageSize > 0 ? PageSize : 10
                );

                ImportHistories = pagedResult.Items.ToList();
                PageNumber = pagedResult.Page;
                PageSize = pagedResult.PageSize;
                TotalPages = pagedResult.TotalPages;
                TotalCount = pagedResult.TotalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading import history");
                ImportHistories = new List<ImportHistory>();
            }
        }
    }
}
