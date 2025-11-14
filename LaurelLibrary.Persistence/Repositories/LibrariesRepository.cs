using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Persistence.Repositories;

public class LibrariesRepository : ILibrariesRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LibrariesRepository> _logger;

    public LibrariesRepository(AppDbContext dbContext, ILogger<LibrariesRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Get list of libraries that belongs to a specific user
    public async Task<List<LibrarySummaryDto>?> GetAllAsync(string userId)
    {
        var libraries = await _dbContext
            .Libraries.Where(l => l.Administrators.Select(x => x.Id).Contains(userId))
            .Select(l => new LibrarySummaryDto
            {
                LibraryId = l.LibraryId,
                Name = l.Name,
                Address = l.Address,
                Alias = l.Alias,
                Description = l.Description,
                BooksCount = l.Books.Count,
                StudentsCount = l.Students.Count,
                AdministratorsCount = l.Administrators.Count,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
            })
            .ToListAsync();
        return libraries;
    }

    public async Task<Library> CreateAsync(Library library)
    {
        library.LibraryId = Guid.NewGuid();
        _dbContext.Libraries.Add(library);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Created new library with ID {LibraryId}", library.LibraryId);
        return library;
    }

    public async Task<Library?> GetByIdAsync(Guid id)
    {
        var result = await _dbContext.Libraries.FirstOrDefaultAsync(l => l.LibraryId == id);
        if (result == null)
        {
            throw new KeyNotFoundException($"Library with ID {id} not found.");
        }

        return result;
    }

    public async Task<Library?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbContext
            .Libraries.Include(l => l.Books)
            .Include(l => l.Students)
            .Include(l => l.Administrators)
            .FirstOrDefaultAsync(l => l.LibraryId == id);
    }

    public async Task<Library?> GetByAliasAsync(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return null;

        var normalizedAlias = alias.Trim();
        return await _dbContext.Libraries.FirstOrDefaultAsync(l => l.Alias == normalizedAlias);
    }

    // Update an existing library
    public async Task<Library?> UpdateAsync(Library library)
    {
        var existingLibrary = await GetByIdAsync(library.LibraryId);
        existingLibrary.Name = library.Name;
        existingLibrary.Address = library.Address;
        existingLibrary.Alias = library.Alias;
        existingLibrary.Logo = library.Logo;
        existingLibrary.PlanningCenterApplicationId = library.PlanningCenterApplicationId;
        existingLibrary.PlanningCenterSecret = library.PlanningCenterSecret;
        existingLibrary.Description = library.Description;
        existingLibrary.CheckoutDurationDays = library.CheckoutDurationDays;
        existingLibrary.UpdatedAt = DateTime.UtcNow;
        existingLibrary.UpdatedBy = library.UpdatedBy;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated library with ID {LibraryId}", existingLibrary.LibraryId);
        return existingLibrary;
    }

    public async Task RemoveAsync(Guid libraryId)
    {
        var existingLibrary = await GetByIdAsync(libraryId);
        _dbContext.Libraries.Remove(existingLibrary);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddAdministratorAsync(Guid libraryId, string userId)
    {
        var library = await _dbContext
            .Libraries.Include(l => l.Administrators)
            .FirstOrDefaultAsync(l => l.LibraryId == libraryId);

        if (library == null)
        {
            throw new KeyNotFoundException($"Library with ID {libraryId} not found.");
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        if (!library.Administrators.Any(a => a.Id == userId))
        {
            library.Administrators.Add(user);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation(
                "Added administrator {UserId} to library {LibraryId}",
                userId,
                libraryId
            );
        }
    }

    public async Task RemoveAdministratorAsync(Guid libraryId, string userId)
    {
        var library = await _dbContext
            .Libraries.Include(l => l.Administrators)
            .FirstOrDefaultAsync(l => l.LibraryId == libraryId);

        if (library == null)
        {
            throw new KeyNotFoundException($"Library with ID {libraryId} not found.");
        }

        var administrator = library.Administrators.FirstOrDefault(a => a.Id == userId);
        if (administrator != null)
        {
            library.Administrators.Remove(administrator);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation(
                "Removed administrator {UserId} from library {LibraryId}",
                userId,
                libraryId
            );
        }
    }

    public async Task<List<Library>> GetLibrariesForUserAsync(string userId)
    {
        return await _dbContext
            .Libraries.Where(l => l.Administrators.Any(a => a.Id == userId))
            .ToListAsync();
    }

    public async Task<int> GetLibraryCountByUserIdAsync(string userId)
    {
        return await _dbContext
            .Libraries.Where(l => l.Administrators.Any(a => a.Id == userId))
            .CountAsync();
    }
}
