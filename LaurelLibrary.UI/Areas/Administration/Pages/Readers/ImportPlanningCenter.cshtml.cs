using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Readers;

[Authorize]
public class ImportPlanningCenterModel : PageModel
{
    private readonly IPlanningCenterService _planningCenterService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly ILogger<ImportPlanningCenterModel> _logger;

    public ImportPlanningCenterModel(
        IPlanningCenterService planningCenterService,
        IAuthenticationService authenticationService,
        ILibrariesRepository librariesRepository,
        ILogger<ImportPlanningCenterModel> logger
    )
    {
        _planningCenterService = planningCenterService;
        _authenticationService = authenticationService;
        _librariesRepository = librariesRepository;
        _logger = logger;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool HasCredentials { get; set; }
    public Guid? CurrentLibraryId { get; set; }
    public PlanningCenterImportSummaryDto? ImportSummary { get; set; }
    public PlanningCenterImportResultDto? ImportResult { get; set; }

    [BindProperty]
    public List<string> SelectedPeopleIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            ErrorMessage = "No library selected. Please select a library first.";
            return Page();
        }

        CurrentLibraryId = currentUser.CurrentLibraryId;

        var library = await _librariesRepository.GetByIdAsync(currentUser.CurrentLibraryId.Value);
        HasCredentials =
            !string.IsNullOrEmpty(library?.PlanningCenterApplicationId)
            && !string.IsNullOrEmpty(library?.PlanningCenterSecret);

        // Load any stored data from TempData
        var importSummaryJson = TempData["ImportSummary"]?.ToString();
        if (!string.IsNullOrEmpty(importSummaryJson))
        {
            ImportSummary =
                System.Text.Json.JsonSerializer.Deserialize<PlanningCenterImportSummaryDto>(
                    importSummaryJson
                );
            // Keep it in TempData for the next request
            TempData["ImportSummary"] = importSummaryJson;
        }

        var importResultJson = TempData["ImportResult"]?.ToString();
        if (!string.IsNullOrEmpty(importResultJson))
        {
            ImportResult =
                System.Text.Json.JsonSerializer.Deserialize<PlanningCenterImportResultDto>(
                    importResultJson
                );
            // Keep it in TempData for the next request
            TempData["ImportResult"] = importResultJson;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostTestConnectionAsync()
    {
        try
        {
            var isConnected = await _planningCenterService.TestConnectionAsync();
            if (isConnected)
            {
                StatusMessage =
                    "✅ Connection successful! Planning Center API is working correctly.";
            }
            else
            {
                ErrorMessage =
                    "❌ Connection failed. Please check your Planning Center credentials.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Planning Center connection");
            ErrorMessage = $"❌ Connection error: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFetchPeopleAsync()
    {
        try
        {
            _logger.LogInformation("Fetching people from Planning Center");
            ImportSummary = await _planningCenterService.GetAllPeopleAsync();

            StatusMessage =
                $"✅ Successfully retrieved {ImportSummary.TotalCount} people from Planning Center. "
                + $"{ImportSummary.PeopleNeedingAttention.Count} people need attention.";

            // Store the summary in TempData for the redirect
            TempData["ImportSummary"] = System.Text.Json.JsonSerializer.Serialize(ImportSummary);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Planning Center configuration issue");
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching people from Planning Center");
            ErrorMessage = $"❌ Error fetching people: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportSelectedAsync()
    {
        try
        {
            if (!SelectedPeopleIds.Any())
            {
                ErrorMessage = "❌ No people selected for import.";
                return RedirectToPage();
            }

            // Get the import summary from TempData
            var importSummaryJson = TempData["ImportSummary"]?.ToString();
            if (string.IsNullOrEmpty(importSummaryJson))
            {
                ErrorMessage = "❌ Import session expired. Please fetch people again.";
                return RedirectToPage();
            }

            ImportSummary =
                System.Text.Json.JsonSerializer.Deserialize<PlanningCenterImportSummaryDto>(
                    importSummaryJson
                );
            if (ImportSummary == null)
            {
                ErrorMessage = "❌ Could not restore import data. Please fetch people again.";
                return RedirectToPage();
            }

            // Filter to only selected people
            var selectedPeople = ImportSummary
                .People.Where(p => SelectedPeopleIds.Contains(p.Id))
                .ToList();

            if (!selectedPeople.Any())
            {
                ErrorMessage = "❌ Selected people not found. Please fetch people again.";
                return RedirectToPage();
            }

            _logger.LogInformation("Importing {Count} selected people", selectedPeople.Count);
            ImportResult = await _planningCenterService.ImportPeopleAsReadersAsync(selectedPeople);

            StatusMessage =
                $"✅ Import completed! Created {ImportResult.SuccessfullyCreated} new readers, "
                + $"updated {ImportResult.Updated} existing readers, and skipped {ImportResult.Skipped}.";

            // Store results for display
            TempData["ImportResult"] = System.Text.Json.JsonSerializer.Serialize(ImportResult);

            // Clear the import summary since we've completed the import
            TempData.Remove("ImportSummary");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing selected people");
            ErrorMessage = $"❌ Import error: {ex.Message}";
        }

        return RedirectToPage();
    }
}
