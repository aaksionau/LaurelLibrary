using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IAuthorsRepository
{
    Task<Author> CreateAsync(Author author);
    Task<Author?> GetByIdAsync(int id);
    Task<Author?> GetByFullNameAsync(string fullName, Guid libraryId);
    Task<IEnumerable<Author>> GetAllAsync(Guid libraryId, int page = 1, int pageSize = 20);
    Task<Author?> UpdateAsync(Author author);
    Task RemoveAsync(int authorId);
}
