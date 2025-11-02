using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class AuthorsService : IAuthorsService
{
    private readonly IAuthorsRepository _authorsRepository;
    private readonly ILogger<AuthorsService> _logger;

    public AuthorsService(IAuthorsRepository authorsRepository, ILogger<AuthorsService> logger)
    {
        _authorsRepository = authorsRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Author>> GetAllAuthorsAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 20
    )
    {
        return await _authorsRepository.GetAllAsync(libraryId, page, pageSize);
    }

    public async Task<Author?> GetAuthorByIdAsync(int id)
    {
        return await _authorsRepository.GetByIdAsync(id);
    }

    public async Task<Author> CreateAuthorAsync(Author author)
    {
        return await _authorsRepository.CreateAsync(author);
    }

    public async Task<Author?> UpdateAuthorAsync(Author author)
    {
        return await _authorsRepository.UpdateAsync(author);
    }

    public async Task RemoveAuthorAsync(int authorId)
    {
        await _authorsRepository.RemoveAsync(authorId);
    }

    public async Task<Author?> GetAuthorByFullNameAsync(string fullName, Guid libraryId)
    {
        return await _authorsRepository.GetByFullNameAsync(fullName, libraryId);
    }
}
