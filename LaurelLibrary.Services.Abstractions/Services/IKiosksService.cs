using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IKiosksService
{
    Task<List<KioskDto>> GetKiosksByLibraryIdAsync(Guid libraryId);
    Task<KioskDto?> GetKioskByIdAsync(int kioskId);
    Task<bool> CreateOrUpdateKioskAsync(KioskDto kioskDto);
    Task<bool> DeleteKioskAsync(int kioskId);
}
