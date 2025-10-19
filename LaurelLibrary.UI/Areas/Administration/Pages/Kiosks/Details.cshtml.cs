using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Kiosks;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IKiosksService _kiosksService;
    private readonly IKiosksRepository _kiosksRepository;

    public DetailsModel(IKiosksService kiosksService, IKiosksRepository kiosksRepository)
    {
        _kiosksService = kiosksService;
        _kiosksRepository = kiosksRepository;
    }

    public KioskDto? Kiosk { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid libraryId, int kioskId)
    {
        var kiosk = await _kiosksService.GetKioskByIdAsync(kioskId);
        if (kiosk == null)
        {
            StatusMessage = "Kiosk not found.";
            return RedirectToPage("/Libraries/Details", new { id = libraryId });
        }

        Kiosk = kiosk;

        // Load audit information
        var kioskEntity = await _kiosksRepository.GetByIdAsync(kioskId);
        if (kioskEntity != null)
        {
            CreatedAt = kioskEntity.CreatedAt;
            UpdatedAt = kioskEntity.UpdatedAt;
            CreatedBy = kioskEntity.CreatedBy;
            UpdatedBy = kioskEntity.UpdatedBy;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateFingerprintAsync(
        [FromBody] UpdateFingerprintRequest request
    )
    {
        try
        {
            var kiosk = await _kiosksRepository.GetByIdAsync(request.KioskId);
            if (kiosk == null)
            {
                return NotFound();
            }

            kiosk.BrowserFingerprint = request.BrowserFingerprint;
            kiosk.UpdatedAt = DateTimeOffset.UtcNow;

            await _kiosksRepository.UpdateAsync(kiosk);

            return new JsonResult(new { success = true });
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

    public class UpdateFingerprintRequest
    {
        public int KioskId { get; set; }
        public string BrowserFingerprint { get; set; } = string.Empty;
    }
}
