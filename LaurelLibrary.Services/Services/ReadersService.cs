using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class ReadersService : IReadersService
{
    private readonly IReadersRepository _readersRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IBooksRepository _booksRepository;
    private readonly IUserService _userService;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<ReadersService> _logger;
    private readonly string? _wwwrootPath;

    public ReadersService(
        IReadersRepository readersRepository,
        ILibrariesRepository librariesRepository,
        IBooksRepository booksRepository,
        IUserService userService,
        IBarcodeService barcodeService,
        ILogger<ReadersService> logger
    )
    {
        _readersRepository = readersRepository;
        _librariesRepository = librariesRepository;
        _booksRepository = booksRepository;
        _userService = userService;
        _barcodeService = barcodeService;
        _logger = logger;
        _wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "reader-eans");
    }

    public async Task<ReaderDto?> GetReaderByIdAsync(int readerId)
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        var entity = await _readersRepository.GetByIdAsync(
            readerId,
            currentUser.CurrentLibraryId.Value
        );
        if (entity == null)
        {
            return null;
        }

        return MapReaderToDto(entity);
    }

    public async Task<ReaderDto?> GetReaderByIdWithoutUserContextAsync(int readerId)
    {
        var entity = await _readersRepository.GetByIdWithoutLibraryAsync(readerId);
        if (entity == null)
        {
            return null;
        }

        return MapReaderToDto(entity);
    }

    public async Task<ReaderDto?> GetReaderByEanAsync(string ean, Guid libraryId)
    {
        var entity = await _readersRepository.GetByEanAsync(ean, libraryId);
        if (entity == null)
        {
            return null;
        }

        return MapReaderToDto(entity);
    }

    public async Task<List<ReaderDto>> GetAllReadersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchName = null
    )
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        var entities = await _readersRepository.GetAllReadersAsync(
            currentUser.CurrentLibraryId.Value,
            page,
            pageSize,
            searchName
        );
        return entities.Select(MapReaderToDto).ToList();
    }

    public async Task<int> GetReadersCountAsync(string? searchName = null)
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        return await _readersRepository.GetReadersCountAsync(
            currentUser.CurrentLibraryId.Value,
            searchName
        );
    }

    public async Task<bool> CreateOrUpdateReaderAsync(ReaderDto readerDto)
    {
        if (readerDto == null)
            throw new ArgumentNullException(nameof(readerDto));

        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser == null)
        {
            throw new InvalidOperationException("Current user not found.");
        }

        if (currentUser.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        var displayName = await GetUserFullNameAsync();

        // Map DTO to entity
        var entity = await MapDtoToEntityAsync(readerDto);

        // Add current user's library to the reader's libraries
        var currentLibrary = await _librariesRepository.GetByIdAsync(
            currentUser.CurrentLibraryId.Value
        );
        if (
            currentLibrary != null
            && !entity.Libraries.Any(l => l.LibraryId == currentLibrary.LibraryId)
        )
        {
            entity.Libraries.Add(currentLibrary);
        }

        // Set audit fields
        entity.UpdatedBy = displayName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        // Check if this is a create or update operation
        if (readerDto.ReaderId == 0)
        {
            // Create new reader
            entity.CreatedBy = displayName;
            entity.CreatedAt = DateTimeOffset.UtcNow;

            // First add the reader to get the ReaderId
            await _readersRepository.AddReaderAsync(entity);
            _logger.LogInformation(
                "Added reader {ReaderId} ({FirstName} {LastName}) with {LibraryCount} libraries",
                entity.ReaderId,
                entity.FirstName,
                entity.LastName,
                entity.Libraries.Count
            );

            // Generate EAN after adding (so we have ReaderId)
            entity.Ean = _barcodeService.GenerateEan13(entity.ReaderId);
            _logger.LogInformation(
                "Generated EAN {Ean} for reader {ReaderId}",
                entity.Ean,
                entity.ReaderId
            );

            // Generate and save barcode image to Azure Blob Storage
            if (!string.IsNullOrEmpty(entity.Ean) && currentLibrary != null)
            {
                var barcodeUrl = await GenerateAndUploadBarcodeAsync(
                    entity.Ean,
                    currentLibrary.LibraryId,
                    entity.ReaderId
                );

                if (barcodeUrl != null)
                {
                    // Update reader with barcode URL
                    await _readersRepository.UpdateBarcodeImageUrlAsync(
                        entity.ReaderId,
                        barcodeUrl
                    );
                }

                // Update reader with EAN using UpdateEanAsync
                await _readersRepository.UpdateEanAsync(entity.ReaderId, entity.Ean);
            }

            _logger.LogInformation(
                "Created reader {ReaderId} with EAN {Ean}",
                entity.ReaderId,
                entity.Ean
            );
            return false; // Created
        }

        // Update existing reader
        var updated = await _readersRepository.UpdateReaderAsync(
            entity,
            currentUser.CurrentLibraryId.Value
        );
        if (updated == null)
        {
            _logger.LogWarning("Reader {ReaderId} not found for update", readerDto.ReaderId);
            return false;
        }

        _logger.LogInformation("Updated reader {ReaderId}", updated.ReaderId);
        return true; // Updated
    }

    public async Task<bool> DeleteReaderAsync(int readerId)
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        var result = await _readersRepository.DeleteReaderAsync(
            readerId,
            currentUser.CurrentLibraryId.Value
        );
        if (result)
        {
            _logger.LogInformation("Deleted reader {ReaderId}", readerId);
        }
        else
        {
            _logger.LogWarning("Reader {ReaderId} not found for deletion", readerId);
        }
        return result;
    }

    private static ReaderDto MapReaderToDto(Reader entity)
    {
        return new ReaderDto
        {
            ReaderId = entity.ReaderId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            DateOfBirth = entity.DateOfBirth,
            Email = entity.Email,
            Ean = entity.Ean,
            BarcodeImageUrl = entity.BarcodeImageUrl,
            LibraryIds = entity.Libraries.Select(l => l.LibraryId).ToList(),
            LibraryNames = entity.Libraries.Select(l => l.Name).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
        };
    }

    private async Task<Reader> MapDtoToEntityAsync(ReaderDto dto)
    {
        var entity = new Reader
        {
            ReaderId = dto.ReaderId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
        };

        // Attach libraries if specified
        if (dto.LibraryIds != null && dto.LibraryIds.Any())
        {
            foreach (var libraryId in dto.LibraryIds)
            {
                var library = await _librariesRepository.GetByIdAsync(libraryId);
                if (library != null)
                {
                    entity.Libraries.Add(library);
                }
            }
        }

        return entity;
    }

    private async Task<string> GetUserFullNameAsync()
    {
        var currentUser = await _userService.GetAppUserAsync();
        var displayName =
            string.IsNullOrWhiteSpace(currentUser?.FirstName)
            && string.IsNullOrWhiteSpace(currentUser?.LastName)
                ? currentUser?.UserName ?? string.Empty
                : $"{currentUser.FirstName} {currentUser.LastName}".Trim();

        return displayName;
    }

    private async Task<string?> GenerateAndUploadBarcodeAsync(
        string ean,
        Guid libraryId,
        int readerId
    )
    {
        var currentLibrary = await _librariesRepository.GetByIdAsync(libraryId);
        if (currentLibrary == null)
        {
            _logger.LogWarning("Library {LibraryId} not found for barcode generation", libraryId);
            return null;
        }

        var blobName = $"{currentLibrary.LibraryId}-{ean}.png";
        _logger.LogInformation(
            "Uploading barcode for reader {ReaderId} with blob name: {BlobName}",
            readerId,
            blobName
        );

        var barcodeUrl = await _barcodeService.GenerateBarcodeImageAsync(ean, blobName);

        if (barcodeUrl == null)
        {
            _logger.LogWarning("Failed to generate barcode for reader {ReaderId}", readerId);
            return null;
        }

        _logger.LogInformation(
            "Barcode uploaded successfully for reader {ReaderId} to {Url}",
            readerId,
            barcodeUrl
        );

        return barcodeUrl;
    }

    public async Task<List<BorrowingHistoryDto>> GetBorrowingHistoryAsync(int readerId)
    {
        var bookInstances = await _booksRepository.GetBorrowingHistoryByReaderIdAsync(readerId);

        return bookInstances
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

    public async Task<bool> RegenerateBarcodeAsync(int readerId)
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        var reader = await _readersRepository.GetByIdAsync(
            readerId,
            currentUser.CurrentLibraryId.Value
        );
        if (reader == null)
        {
            _logger.LogWarning("Reader {ReaderId} not found for barcode regeneration", readerId);
            return false;
        }

        if (string.IsNullOrEmpty(reader.Ean))
        {
            _logger.LogWarning(
                "Reader {ReaderId} does not have an EAN, cannot regenerate barcode",
                readerId
            );
            return false;
        }

        // Generate and save barcode image to Azure Blob Storage
        var barcodeUrl = await GenerateAndUploadBarcodeAsync(
            reader.Ean,
            currentUser.CurrentLibraryId.Value,
            readerId
        );

        if (barcodeUrl == null)
        {
            return false;
        }

        _logger.LogInformation("Barcode regenerated successfully for reader {ReaderId}", readerId);

        // Update reader with new barcode URL
        await _readersRepository.UpdateBarcodeImageUrlAsync(readerId, barcodeUrl);

        return true;
    }
}
