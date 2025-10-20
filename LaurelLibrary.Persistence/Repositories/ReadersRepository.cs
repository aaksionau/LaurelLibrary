using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class ReadersRepository : IReadersRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IUserService _userService;

    public ReadersRepository(AppDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    private async Task<List<Guid>> GetUserAdministeredLibraryIdsAsync()
    {
        var currentUser = await _userService.GetAppUserAsync();
        return await _dbContext
            .Libraries.Where(l => l.Administrators.Any(a => a.Id == currentUser.Id))
            .Select(l => l.LibraryId)
            .ToListAsync();
    }

    public async Task AddReaderAsync(Reader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        await _dbContext.Readers.AddAsync(reader);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateEanAsync(int readerId, string ean)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var reader = await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == readerId && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
            );

        if (reader != null)
        {
            reader.Ean = ean;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateBarcodeImageUrlAsync(int readerId, string barcodeImageUrl)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var reader = await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == readerId && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
            );

        if (reader != null)
        {
            reader.BarcodeImageUrl = barcodeImageUrl;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<Reader?> UpdateReaderAsync(Reader reader, Guid libraryId)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var existing = await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == reader.ReaderId
                && r.Libraries.Any(l => l.LibraryId == libraryId)
                && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
            );

        if (existing == null)
        {
            return null;
        }

        // Update scalar properties
        existing.FirstName = reader.FirstName;
        existing.LastName = reader.LastName;
        existing.DateOfBirth = reader.DateOfBirth;
        existing.Ean = reader.Ean;
        existing.BarcodeImageUrl = reader.BarcodeImageUrl;

        // Replace libraries
        existing.Libraries.Clear();
        foreach (var library in reader.Libraries)
        {
            existing.Libraries.Add(library);
        }

        // Persist audit fields if provided
        existing.UpdatedAt = reader.UpdatedAt;
        existing.UpdatedBy = reader.UpdatedBy;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<Reader?> GetByIdAsync(int readerId, Guid libraryId)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        return await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == readerId
                && r.Libraries.Any(l => l.LibraryId == libraryId)
                && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
            );
    }

    public async Task<Reader?> GetByEanAsync(string ean, Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(ean))
            return null;

        return await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.Ean == ean.Trim() && r.Libraries.Any(l => l.LibraryId == libraryId)
            );
    }

    public async Task<List<Reader>> GetAllReadersAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 10,
        string? searchName = null
    )
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var query = _dbContext
            .Readers.Include(r => r.Libraries)
            .Where(r =>
                r.Libraries.Any(l => l.LibraryId == libraryId)
                && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
            );

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var pattern = "%" + searchName.Trim() + "%";
            query = query.Where(r =>
                EF.Functions.Like(r.FirstName, pattern) || EF.Functions.Like(r.LastName, pattern)
            );
        }

        var readers = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return readers;
    }

    public async Task<int> GetReadersCountAsync(Guid libraryId, string? searchName = null)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var query = _dbContext.Readers.Where(r =>
            r.Libraries.Any(l => l.LibraryId == libraryId)
            && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
        );

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var pattern = "%" + searchName.Trim() + "%";
            query = query.Where(r =>
                EF.Functions.Like(r.FirstName, pattern) || EF.Functions.Like(r.LastName, pattern)
            );
        }

        return await query.CountAsync();
    }

    public async Task<bool> DeleteReaderAsync(int readerId, Guid libraryId)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var existing = await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == readerId
                && r.Libraries.Any(l => l.LibraryId == libraryId)
                && r.Libraries.Any(l => userLibraryIds.Contains(l.LibraryId))
            );

        if (existing == null)
            return false;

        // Clear libraries relationships
        existing.Libraries.Clear();

        _dbContext.Readers.Remove(existing);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
