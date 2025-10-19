using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface ILibrariesRepository
{
    Task<List<LibrarySummaryDto>?> GetAllAsync(string userId);
    Task<Library> CreateAsync(Library library);
    Task<Library?> GetByIdAsync(Guid id);
    Task<Library?> GetByIdWithDetailsAsync(Guid id);
    Task<Library?> UpdateAsync(Library library);
    Task RemoveAsync(Guid libraryId);
}
