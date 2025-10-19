using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class ReadersRepository : IReadersRepository
{
    private readonly AppDbContext _dbContext;

    public ReadersRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
        var reader = await _dbContext.Readers.FindAsync(readerId);
        if (reader != null)
        {
            reader.Ean = ean;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateBarcodeImageUrlAsync(int readerId, string barcodeImageUrl)
    {
        var reader = await _dbContext.Readers.FindAsync(readerId);
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

        var existing = await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == reader.ReaderId && r.Libraries.Any(l => l.LibraryId == libraryId)
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
        return await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == readerId && r.Libraries.Any(l => l.LibraryId == libraryId)
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
        var query = _dbContext
            .Readers.Include(r => r.Libraries)
            .Where(r => r.Libraries.Any(l => l.LibraryId == libraryId));

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
        var query = _dbContext.Readers.Where(r => r.Libraries.Any(l => l.LibraryId == libraryId));

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
        var existing = await _dbContext
            .Readers.Include(r => r.Libraries)
            .FirstOrDefaultAsync(r =>
                r.ReaderId == readerId && r.Libraries.Any(l => l.LibraryId == libraryId)
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
