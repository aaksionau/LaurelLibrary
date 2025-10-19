using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Persistence.Repositories;

public class KiosksRepository : IKiosksRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<KiosksRepository> _logger;

    public KiosksRepository(AppDbContext dbContext, ILogger<KiosksRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<KioskDto>> GetAllByLibraryIdAsync(Guid libraryId)
    {
        return await _dbContext
            .Kiosks.Where(k => k.LibraryId == libraryId)
            .Select(k => new KioskDto
            {
                KioskId = k.KioskId,
                Location = k.Location,
                BrowserFingerprint = k.BrowserFingerprint,
                LibraryId = k.LibraryId,
                LibraryName = k.Library.Name,
            })
            .ToListAsync();
    }

    public async Task<Kiosk?> GetByIdAsync(int kioskId)
    {
        return await _dbContext
            .Kiosks.Include(k => k.Library)
            .FirstOrDefaultAsync(k => k.KioskId == kioskId);
    }

    public async Task<Kiosk> CreateAsync(Kiosk kiosk)
    {
        kiosk.CreatedAt = DateTimeOffset.UtcNow;
        kiosk.UpdatedAt = DateTimeOffset.UtcNow;
        _dbContext.Kiosks.Add(kiosk);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Created new kiosk with ID {KioskId}", kiosk.KioskId);
        return kiosk;
    }

    public async Task<Kiosk?> UpdateAsync(Kiosk kiosk)
    {
        var existingKiosk = await GetByIdAsync(kiosk.KioskId);
        if (existingKiosk == null)
        {
            _logger.LogWarning("Kiosk with ID {KioskId} not found for update", kiosk.KioskId);
            return null;
        }

        existingKiosk.Location = kiosk.Location;
        existingKiosk.BrowserFingerprint = kiosk.BrowserFingerprint;
        existingKiosk.UpdatedAt = DateTimeOffset.UtcNow;
        existingKiosk.UpdatedBy = kiosk.UpdatedBy;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated kiosk with ID {KioskId}", existingKiosk.KioskId);
        return existingKiosk;
    }

    public async Task RemoveAsync(int kioskId)
    {
        var kiosk = await GetByIdAsync(kioskId);
        if (kiosk != null)
        {
            _dbContext.Kiosks.Remove(kiosk);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Deleted kiosk with ID {KioskId}", kioskId);
        }
    }
}
