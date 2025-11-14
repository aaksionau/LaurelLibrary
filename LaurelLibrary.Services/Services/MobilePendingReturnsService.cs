using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class MobilePendingReturnsService : IMobilePendingReturnsService
{
    private readonly IPendingReturnsRepository _pendingReturnsRepository;
    private readonly IBooksRepository _booksRepository;
    private readonly IReaderKioskService _readerKioskService;
    private readonly ILogger<MobilePendingReturnsService> _logger;

    public MobilePendingReturnsService(
        IPendingReturnsRepository pendingReturnsRepository,
        IBooksRepository booksRepository,
        IReaderKioskService readerKioskService,
        ILogger<MobilePendingReturnsService> logger
    )
    {
        _pendingReturnsRepository = pendingReturnsRepository;
        _booksRepository = booksRepository;
        _readerKioskService = readerKioskService;
        _logger = logger;
    }

    public async Task<List<PendingReturnDto>> GetPendingReturnsAsync(Guid libraryId)
    {
        try
        {
            var pendingReturns = await _pendingReturnsRepository.GetPendingReturnsByLibraryIdAsync(
                libraryId,
                PendingReturnStatus.Pending
            );

            return pendingReturns
                .Select(pr => new PendingReturnDto
                {
                    PendingReturnId = pr.PendingReturnId,
                    ReaderId = pr.ReaderId,
                    ReaderName = $"{pr.Reader.FirstName} {pr.Reader.LastName}",
                    ReaderEmail = pr.Reader.Email,
                    LibraryId = pr.LibraryId,
                    LibraryName = pr.Library.Name,
                    RequestedAt = pr.RequestedAt,
                    Notes = pr.Notes,
                    Status = pr.Status.ToString(),
                    Books = pr
                        .Items.Select(item => new ReturnBookInstanceDto
                        {
                            BookInstanceId = item.BookInstanceId,
                            BookId = item.BookInstance.BookId,
                            BookTitle = item.BookInstance.Book.Title,
                            BookAuthors = string.Join(
                                ", ",
                                item.BookInstance.Book.Authors.Select(a => a.FullName)
                            ),
                            Status = item.BookInstance.Status,
                            BorrowedByReader = $"{pr.Reader.FirstName} {pr.Reader.LastName}",
                            CheckedOutDate = item.BookInstance.CheckedOutDate,
                            DueDate = item.BookInstance.DueDate,
                        })
                        .ToList(),
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting pending returns for library: {LibraryId}",
                libraryId
            );
            throw;
        }
    }

    public async Task<bool> ApprovePendingReturnAsync(int pendingReturnId, string approvedByUserId)
    {
        try
        {
            var pendingReturn = await _pendingReturnsRepository.GetPendingReturnByIdAsync(
                pendingReturnId
            );
            if (pendingReturn == null)
            {
                _logger.LogWarning("Pending return not found: {PendingReturnId}", pendingReturnId);
                return false;
            }

            // Process the actual return using the kiosk service
            var bookInstanceIds = pendingReturn.Items.Select(item => item.BookInstanceId).ToList();
            var returnSuccess = await _readerKioskService.ReturnBooksAsync(
                bookInstanceIds,
                pendingReturn.LibraryId
            );

            if (returnSuccess)
            {
                // Update the pending return status to approved
                await _pendingReturnsRepository.UpdatePendingReturnStatusAsync(
                    pendingReturnId,
                    PendingReturnStatus.Approved,
                    approvedByUserId
                );

                _logger.LogInformation(
                    "Approved pending return {PendingReturnId} for reader {ReaderId} by user {UserId}",
                    pendingReturnId,
                    pendingReturn.ReaderId,
                    approvedByUserId
                );

                return true;
            }
            else
            {
                _logger.LogError(
                    "Failed to process actual book return for pending return: {PendingReturnId}",
                    pendingReturnId
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error approving pending return: {PendingReturnId}",
                pendingReturnId
            );
            throw;
        }
    }

    public async Task<bool> RejectPendingReturnAsync(int pendingReturnId, string approvedByUserId)
    {
        try
        {
            var pendingReturn = await _pendingReturnsRepository.UpdatePendingReturnStatusAsync(
                pendingReturnId,
                PendingReturnStatus.Rejected,
                approvedByUserId
            );

            if (pendingReturn != null)
            {
                _logger.LogInformation(
                    "Rejected pending return {PendingReturnId} for reader {ReaderId} by user {UserId}",
                    pendingReturnId,
                    pendingReturn.ReaderId,
                    approvedByUserId
                );
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rejecting pending return: {PendingReturnId}",
                pendingReturnId
            );
            throw;
        }
    }
}
