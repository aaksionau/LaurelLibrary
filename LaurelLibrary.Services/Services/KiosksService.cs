using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class KiosksService : IKiosksService
{
    private readonly IKiosksRepository _kiosksRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<KiosksService> _logger;

    public KiosksService(
        IKiosksRepository kiosksRepository,
        IAuthenticationService authenticationService,
        ILogger<KiosksService> logger
    )
    {
        _kiosksRepository = kiosksRepository;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<List<KioskDto>> GetKiosksByLibraryIdAsync(Guid libraryId)
    {
        return await _kiosksRepository.GetAllByLibraryIdAsync(libraryId);
    }

    public async Task<KioskDto?> GetKioskByIdAsync(int kioskId)
    {
        var kiosk = await _kiosksRepository.GetByIdAsync(kioskId);
        if (kiosk == null)
        {
            return null;
        }

        return new KioskDto
        {
            KioskId = kiosk.KioskId,
            Location = kiosk.Location,
            BrowserFingerprint = kiosk.BrowserFingerprint,
            LibraryId = kiosk.LibraryId,
            LibraryName = kiosk.Library?.Name,
        };
    }

    public async Task<bool> CreateOrUpdateKioskAsync(KioskDto kioskDto)
    {
        try
        {
            var currentUser = await _authenticationService.GetAppUserAsync();
            var displayName =
                string.IsNullOrWhiteSpace(currentUser?.FirstName)
                && string.IsNullOrWhiteSpace(currentUser?.LastName)
                    ? currentUser?.UserName ?? string.Empty
                    : $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            var entity = new Kiosk
            {
                KioskId = kioskDto.KioskId,
                Location = kioskDto.Location,
                BrowserFingerprint = kioskDto.BrowserFingerprint,
                LibraryId = kioskDto.LibraryId,
                UpdatedBy = displayName,
            };

            if (kioskDto.KioskId == 0)
            {
                entity.CreatedBy = displayName;
                await _kiosksRepository.CreateAsync(entity);
            }
            else
            {
                await _kiosksRepository.UpdateAsync(entity);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating kiosk");
            return false;
        }
    }

    public async Task<bool> DeleteKioskAsync(int kioskId)
    {
        try
        {
            await _kiosksRepository.RemoveAsync(kioskId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting kiosk {KioskId}", kioskId);
            return false;
        }
    }
}
