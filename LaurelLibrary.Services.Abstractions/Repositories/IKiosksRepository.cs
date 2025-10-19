using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IKiosksRepository
{
    Task<List<KioskDto>> GetAllByLibraryIdAsync(Guid libraryId);
    Task<Kiosk?> GetByIdAsync(int kioskId);
    Task<Kiosk> CreateAsync(Kiosk kiosk);
    Task<Kiosk?> UpdateAsync(Kiosk kiosk);
    Task RemoveAsync(int kioskId);
}
