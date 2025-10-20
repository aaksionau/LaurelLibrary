using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface ILibrariesService
{
    Task<Library?> GetLibraryByIdWithDetailsAsync(Guid libraryId);
    Task<bool> AddAdministratorByEmailAsync(Guid libraryId, string email);
    Task<bool> RemoveAdministratorAsync(Guid libraryId, string userId);
}
