using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.AuditLogs;

[Authorize]
public class ListModel : PageModel
{
    private readonly IAuditLogService _auditLogService;
    private readonly IAuthenticationService _authenticationService;

    public ListModel(IAuditLogService auditLogService, IAuthenticationService authenticationService)
    {
        _auditLogService = auditLogService;
        _authenticationService = authenticationService;
    }

    public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityTypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public async Task OnGetAsync(int pageNumber = 1, int pageSize = 50)
    {
        CurrentPage = pageNumber;
        PageSize = pageSize;

        try
        {
            var currentUser = await _authenticationService.GetAppUserAsync();
            if (currentUser?.CurrentLibraryId == null)
            {
                // Handle case where user doesn't have a current library
                AuditLogs = new List<AuditLog>();
                TotalCount = 0;
                TempData["StatusMessage"] = "Please select a library to view audit logs.";
                return;
            }

            AuditLogs = await _auditLogService.GetAuditLogsAsync(
                currentUser.CurrentLibraryId.Value,
                CurrentPage,
                PageSize,
                ActionFilter,
                EntityTypeFilter,
                StartDate,
                EndDate
            );

            TotalCount = await _auditLogService.GetAuditLogsCountAsync(
                currentUser.CurrentLibraryId.Value,
                ActionFilter,
                EntityTypeFilter,
                StartDate,
                EndDate
            );
        }
        catch (Exception)
        {
            // Handle the case where user doesn't have a current library
            // This can happen if they're not properly authenticated or don't have library access
            AuditLogs = new List<AuditLog>();
            TotalCount = 0;
            TempData["StatusMessage"] =
                "Unable to load audit logs. Please ensure you have selected a library.";
        }
    }
}
