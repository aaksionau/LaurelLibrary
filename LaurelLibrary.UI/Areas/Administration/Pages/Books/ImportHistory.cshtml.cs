using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    public class ImportHistoryModel : PageModel
    {
        private readonly IBookImportService _bookImportService;
        private readonly BookImportJobService _bookImportJobService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IUserService _userService;
        private readonly ILogger<ImportHistoryModel> _logger;

        public ImportHistoryModel(
            IBookImportService bookImportService,
            BookImportJobService bookImportJobService,
            IAuthenticationService authenticationService,
            IUserService userService,
            ILogger<ImportHistoryModel> logger
        )
        {
            _bookImportService = bookImportService;
            _bookImportJobService = bookImportJobService;
            _authenticationService = authenticationService;
            _userService = userService;
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
        public string? HangfireJobId { get; set; }

        // Add property to track current tab
        public string? CurrentTab { get; set; }

        // Property to track if user is administrator
        public bool IsUserAdministrator { get; set; }

        public async Task OnGetAsync(int? pageNumber, int? pageSize, string? tab)
        {
            // Store the current tab
            CurrentTab = tab;

            // Check if user is administrator
            await SetUserAdministratorStatusAsync();

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

            try
            {
                var importHistory = await _bookImportService.ImportBooksFromCsvAsync(CsvFile);

                ImportHistoryId = importHistory.ImportHistoryId;
                TotalIsbns = importHistory.TotalIsbns;
                SuccessCount = importHistory.SuccessCount;
                FailedCount = importHistory.FailedCount;

                // Enqueue the Hangfire job to process the import
                var jobId = _bookImportJobService.EnqueueImportJob(importHistory.ImportHistoryId);
                HangfireJobId = jobId;

                Message =
                    $"Import started successfully! Processing {TotalIsbns} ISBNs in the background. Job ID: {jobId}";
                IsSuccess = true;

                _logger.LogInformation(
                    "Bulk import started: {ImportId}, Total ISBNs: {Total}, Hangfire Job ID: {JobId}",
                    importHistory.ImportHistoryId,
                    TotalIsbns,
                    jobId
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file provided for bulk import");
                Message = ex.Message;
                IsSuccess = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                Message = $"An error occurred during import: {ex.Message}";
                IsSuccess = false;
            }

            await SetUserAdministratorStatusAsync();
            await LoadImportHistoryAsync();
            return Page();
        }

        private async Task SetUserAdministratorStatusAsync()
        {
            try
            {
                var currentUser = await _authenticationService.GetAppUserAsync();
                IsUserAdministrator = await _userService.HasAdministratorClaimAsync(currentUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user administrator status");
                IsUserAdministrator = false;
            }
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

            // Ensure administrator status is set
            await SetUserAdministratorStatusAsync();
        }
    }
}
