using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IAuthorsService
{
    /// <summary>
    /// Gets all authors for a library with pagination.
    /// </summary>
    Task<IEnumerable<Author>> GetAllAuthorsAsync(Guid libraryId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets an author by ID.
    /// </summary>
    Task<Author?> GetAuthorByIdAsync(int id);

    /// <summary>
    /// Creates a new author.
    /// </summary>
    Task<Author> CreateAuthorAsync(Author author);

    /// <summary>
    /// Updates an existing author.
    /// </summary>
    Task<Author?> UpdateAuthorAsync(Author author);

    /// <summary>
    /// Deletes an author.
    /// </summary>
    Task RemoveAuthorAsync(int authorId);

    /// <summary>
    /// Gets an author by full name for a specific library.
    /// </summary>
    Task<Author?> GetAuthorByFullNameAsync(string fullName, Guid libraryId);
}
