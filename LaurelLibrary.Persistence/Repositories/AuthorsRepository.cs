using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Persistence.Repositories;

public class AuthorsRepository : IAuthorsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuthorsRepository> _logger;

    public AuthorsRepository(AppDbContext dbContext, ILogger<AuthorsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Author> CreateAsync(Author author)
    {
        if (author == null)
            throw new ArgumentNullException(nameof(author));

        author.AuthorId = 0; // Ensure EF will insert
        await _dbContext.Authors.AddAsync(author);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation(
            "Created author {AuthorId} for library {LibraryId}",
            author.AuthorId,
            author.LibraryId
        );
        return author;
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        var author = await _dbContext
            .Authors.Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.AuthorId == id);

        if (author == null)
        {
            _logger.LogDebug("Author with id {AuthorId} not found", id);
        }

        return author;
    }

    public async Task<Author?> GetByFullNameAsync(string fullName, Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var normalized = fullName.Trim();
        var author = await _dbContext.Authors.FirstOrDefaultAsync(a =>
            a.LibraryId == libraryId && a.FullName == normalized
        );

        return author;
    }

    public async Task<IEnumerable<Author>> GetAllAsync(
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
            .Authors.Where(a => a.LibraryId == libraryId)
            .OrderBy(a => a.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return result;
    }

    public async Task<Author?> UpdateAsync(Author author)
    {
        if (author == null)
            throw new ArgumentNullException(nameof(author));

        var existing = await _dbContext.Authors.FirstOrDefaultAsync(a =>
            a.AuthorId == author.AuthorId && a.LibraryId == author.LibraryId
        );
        if (existing == null)
            return null;

        existing.FullName = author.FullName;
        // Books relationship will be managed elsewhere (e.g. BooksRepository) to avoid complex many-to-many replace here

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated author {AuthorId}", existing.AuthorId);
        return existing;
    }

    public async Task RemoveAsync(int authorId)
    {
        var existing = await _dbContext.Authors.FirstOrDefaultAsync(a => a.AuthorId == authorId);
        if (existing == null)
        {
            _logger.LogWarning("Attempted to remove non-existing author {AuthorId}", authorId);
            return;
        }

        _dbContext.Authors.Remove(existing);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Removed author {AuthorId}", authorId);
    }

    public async Task<IEnumerable<Author>> SearchByNameAsync(
        string searchTerm,
        Guid libraryId,
        int limit = 10
    )
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Author>();

        if (limit < 1)
            limit = 10;

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        var result = await _dbContext
            .Authors.Where(a =>
                a.LibraryId == libraryId && a.FullName.ToLower().Contains(normalizedSearchTerm)
            )
            .OrderBy(a => a.FullName)
            .Take(limit)
            .ToListAsync();

        return result;
    }
}
