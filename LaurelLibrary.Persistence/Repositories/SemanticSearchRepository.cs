using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class SemanticSearchRepository : ISemanticSearchRepository
{
    private readonly AppDbContext _dbContext;

    public SemanticSearchRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<LaurelBookSummaryDto>> SearchBooksAsync(
        SearchCriteria criteria,
        Guid libraryId,
        int page = 1,
        int pageSize = 10
    )
    {
        var query = BuildEntityQuery(criteria, libraryId);
        var totalCount = await query.CountAsync();

        var books = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryBookDto())
            .ToListAsync();

        return new PagedResult<LaurelBookSummaryDto>
        {
            Items = books,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    private IQueryable<Book> BuildEntityQuery(SearchCriteria criteria, Guid libraryId)
    {
        var query = _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.BookInstances)
            .Where(b => b.LibraryId == libraryId);

        // Title keywords
        if (criteria.TitleKeywords?.Any() == true)
        {
            foreach (var keyword in criteria.TitleKeywords)
            {
                query = query.Where(b => b.Title.Contains(keyword));
            }
        }

        // Author keywords
        if (criteria.AuthorKeywords?.Any() == true)
        {
            foreach (var keyword in criteria.AuthorKeywords)
            {
                query = query.Where(b => b.Authors.Any(a => a.FullName.Contains(keyword)));
            }
        }

        // Category keywords
        if (criteria.CategoryKeywords?.Any() == true)
        {
            foreach (var keyword in criteria.CategoryKeywords)
            {
                query = query.Where(b => b.Categories.Any(c => c.Name.Contains(keyword)));
            }
        }

        // Synopsis keywords
        if (criteria.SynopsisKeywords?.Any() == true)
        {
            foreach (var keyword in criteria.SynopsisKeywords)
            {
                query = query.Where(b => b.Synopsis != null && b.Synopsis.Contains(keyword));
            }
        }

        // Age range
        if (criteria.MinAge.HasValue)
        {
            query = query.Where(b => b.MaxAge >= criteria.MinAge.Value);
        }
        if (criteria.MaxAge.HasValue)
        {
            query = query.Where(b => b.MinAge <= criteria.MaxAge.Value);
        }

        // Language
        if (!string.IsNullOrEmpty(criteria.Language))
        {
            query = query.Where(b => b.Language != null && b.Language.Contains(criteria.Language));
        }

        // Page count
        if (criteria.MinPages.HasValue)
        {
            query = query.Where(b => b.Pages >= criteria.MinPages.Value);
        }
        if (criteria.MaxPages.HasValue)
        {
            query = query.Where(b => b.Pages <= criteria.MaxPages.Value);
        }

        // Publication date
        if (criteria.PublishedAfter.HasValue)
        {
            query = query.Where(b => b.DatePublished >= criteria.PublishedAfter.Value);
        }
        if (criteria.PublishedBefore.HasValue)
        {
            query = query.Where(b => b.DatePublished <= criteria.PublishedBefore.Value);
        }

        return query.OrderBy(b => b.Title);
    }
}
