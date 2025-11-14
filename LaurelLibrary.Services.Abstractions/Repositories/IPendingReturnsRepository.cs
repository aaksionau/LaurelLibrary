using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IPendingReturnsRepository
{
    Task<PendingReturn> CreatePendingReturnAsync(PendingReturn pendingReturn);
    Task<PendingReturn?> GetPendingReturnByIdAsync(int pendingReturnId);
    Task<List<PendingReturn>> GetPendingReturnsByLibraryIdAsync(
        Guid libraryId,
        PendingReturnStatus? status = null
    );
    Task<List<PendingReturn>> GetPendingReturnsByReaderIdAsync(int readerId);
    Task<PendingReturn?> UpdatePendingReturnStatusAsync(
        int pendingReturnId,
        PendingReturnStatus status,
        string? approvedByUserId = null
    );
    Task<bool> DeletePendingReturnAsync(int pendingReturnId);
}
