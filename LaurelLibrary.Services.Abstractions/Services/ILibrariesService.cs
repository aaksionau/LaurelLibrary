using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface ILibrariesService
{
    Task<Library?> GetLibraryByIdWithDetailsAsync(Guid libraryId);
    Task<bool> AddAdministratorByEmailAsync(Guid libraryId, string email);
    Task<bool> RemoveAdministratorAsync(Guid libraryId, string userId);
    Task<List<LibraryDto>> GetLibrariesForUserAsync(string userId);
    Task<Library> CreateLibraryAsync(LibraryDto libraryDto, string userId);
    Task<bool> DeleteLibraryAsync(Guid libraryId);
}
