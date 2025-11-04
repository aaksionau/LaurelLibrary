using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Persistence.Repositories;

public class CategoriesRepository : ICategoriesRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CategoriesRepository> _logger;

    public CategoriesRepository(AppDbContext dbContext, ILogger<CategoriesRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Category> CreateAsync(Category category)
    {
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        category.CategoryId = 0; // ensure EF will insert
        await _dbContext.Categories.AddAsync(category);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation(
            "Created category {CategoryId} in library {LibraryId}",
            category.CategoryId,
            category.LibraryId
        );
        return category;
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            _logger.LogDebug("Category with id {CategoryId} not found", id);
        }

        return category;
    }

    public async Task<Category?> GetByNameAsync(string name, Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = name.Trim();
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c =>
            c.LibraryId == libraryId && c.Name == normalized
        );
        return category;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1)
            page = 1;
        if (pageSize < 1)
            pageSize = 20;

        var result = await _dbContext
            .Categories.Where(c => c.LibraryId == libraryId)
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return result;
    }

    public async Task<Category?> UpdateAsync(Category category)
    {
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        var existing = await _dbContext.Categories.FirstOrDefaultAsync(c =>
            c.CategoryId == category.CategoryId && c.LibraryId == category.LibraryId
        );
        if (existing == null)
            return null;

        existing.Name = category.Name;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated category {CategoryId}", existing.CategoryId);
        return existing;
    }

    public async Task RemoveAsync(int categoryId)
    {
        var existing = await _dbContext.Categories.FirstOrDefaultAsync(c =>
            c.CategoryId == categoryId
        );
        if (existing == null)
        {
            _logger.LogWarning(
                "Attempted to remove non-existing category {CategoryId}",
                categoryId
            );
            return;
        }

        _dbContext.Categories.Remove(existing);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Removed category {CategoryId}", categoryId);
    }

    public async Task<IEnumerable<Category>> SearchByNameAsync(
        string searchTerm,
        Guid libraryId,
        int limit = 10
    )
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Category>();

        if (limit < 1)
            limit = 10;

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        var result = await _dbContext
            .Categories.Where(c =>
                c.LibraryId == libraryId && c.Name.ToLower().Contains(normalizedSearchTerm)
            )
            .OrderBy(c => c.Name)
            .Take(limit)
            .ToListAsync();

        return result;
    }
}
