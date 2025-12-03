using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class MobileReaderService : IMobileReaderService
{
    private readonly IReadersRepository _readersRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IBooksRepository _booksRepository;
    private readonly ILogger<MobileReaderService> _logger;

    public MobileReaderService(
        IReadersRepository readersRepository,
        ILibrariesRepository librariesRepository,
        IBooksRepository booksRepository,
        ILogger<MobileReaderService> logger
    )
    {
        _readersRepository = readersRepository;
        _librariesRepository = librariesRepository;
        _booksRepository = booksRepository;
        _logger = logger;
    }

    public async Task<MobileReaderVerificationResponseDto> VerifyReaderAsync(
        MobileReaderVerificationRequestDto request
    )
    {
        try
        {
            // First, verify library exists
            var library = await _librariesRepository.GetByIdAsync(request.LibraryId);
            if (library == null)
            {
                return new MobileReaderVerificationResponseDto
                {
                    IsVerified = false,
                    Message = "Library not found",
                };
            }

            // Find reader by email in the specified library
            var readers = await _readersRepository.GetAllReadersAsync(request.LibraryId, 1, 100);
            var reader = readers.FirstOrDefault(r =>
                r.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)
            );

            if (reader == null)
            {
                return new MobileReaderVerificationResponseDto
                {
                    IsVerified = false,
                    Message =
                        $"No reader found with email '{request.Email}' in library '{library.Name}'",
                };
            }

            return new MobileReaderVerificationResponseDto
            {
                IsVerified = true,
                Reader = reader.ToReaderDto(),
                Message = $"Reader verified successfully in library '{library.Name}'",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error verifying reader with email: {Email} in library: {LibraryId}",
                request.Email,
                request.LibraryId
            );
            return new MobileReaderVerificationResponseDto
            {
                IsVerified = false,
                Message = "Error occurred during verification",
            };
        }
    }

    public async Task<MobileReaderInfoDto?> GetReaderInfoAsync(int readerId, Guid libraryId)
    {
        try
        {
            var reader = await _readersRepository.GetByIdAsync(readerId, libraryId);
            if (reader == null)
                return null;

            // Get borrowing history (current borrowed books)
            var borrowingHistory = await _booksRepository.GetBorrowingHistoryByReaderIdAsync(
                libraryId,
                readerId
            );
            var currentBorrowedBooks = borrowingHistory
                .Where(bh => bh.Status == Domain.Enums.BookInstanceStatus.Borrowed)
                .Select(bi => new BorrowingHistoryDto
                {
                    BookInstanceId = bi.BookInstanceId,
                    BookUrl = bi.Book.Image ?? string.Empty,
                    BookTitle = bi.Book.Title,
                    BookIsbn = bi.Book.Isbn ?? string.Empty,
                    AuthorNames = string.Join(", ", bi.Book.Authors.Select(a => a.FullName)),
                    CheckedOutDate = bi.CheckedOutDate,
                    DueDate = bi.DueDate,
                    IsCurrentlyBorrowed = true,
                })
                .ToList();

            // Calculate overdue books
            var overdueBooks = currentBorrowedBooks.Count(b =>
                b.DueDate.HasValue && b.DueDate.Value < DateTimeOffset.UtcNow
            );

            // Get library info
            var libraries = new List<MobileLibraryDto>();
            foreach (var lib in reader.Libraries)
            {
                libraries.Add(
                    new MobileLibraryDto
                    {
                        LibraryId = lib.LibraryId,
                        Name = lib.Name,
                        Description = lib.Description,
                        Address = lib.Address,
                        CheckoutDurationDays = lib.CheckoutDurationDays,
                    }
                );
            }

            return new MobileReaderInfoDto
            {
                Reader = reader.ToReaderDto(),
                Libraries = libraries,
                CurrentBorrowedBooks = currentBorrowedBooks,
                TotalBooksCheckedOut = currentBorrowedBooks.Count,
                OverdueBooks = overdueBooks,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting reader info for reader: {ReaderId} in library: {LibraryId}",
                readerId,
                libraryId
            );
            throw;
        }
    }

    public async Task<List<BorrowingHistoryDto>> GetReaderHistoryAsync(
        Guid libraryId,
        int readerId,
        int page = 1,
        int pageSize = 50
    )
    {
        try
        {
            var bookInstances = await _booksRepository.GetBorrowingHistoryByReaderIdAsync(libraryId, readerId);

            return bookInstances
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(bi => new BorrowingHistoryDto
                {
                    BookInstanceId = bi.BookInstanceId,
                    BookTitle = bi.Book.Title,
                    BookIsbn = bi.Book.Isbn ?? string.Empty,
                    AuthorNames = string.Join(", ", bi.Book.Authors.Select(a => a.FullName)),
                    CheckedOutDate = bi.CheckedOutDate,
                    DueDate = bi.DueDate,
                    IsCurrentlyBorrowed =
                        bi.Status == Domain.Enums.BookInstanceStatus.Borrowed
                        && bi.ReaderId == readerId,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reader history for reader: {ReaderId}", readerId);
            throw;
        }
    }
}
