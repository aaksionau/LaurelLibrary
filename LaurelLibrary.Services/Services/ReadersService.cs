using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Exceptions;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
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
    private readonly IAuthenticationService _authenticationService;
    private readonly IBarcodeService _barcodeService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly IReaderActionService _readerActionService;
    private readonly ILogger<ReadersService> _logger;

    public ReadersService(
        IReadersRepository readersRepository,
        ILibrariesRepository librariesRepository,
        IBooksRepository booksRepository,
        IUserService userService,
        IAuthenticationService authenticationService,
        IBarcodeService barcodeService,
        IBlobStorageService blobStorageService,
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService,
        IReaderActionService readerActionService,
        ILogger<ReadersService> logger
    )
    {
        _readersRepository = readersRepository;
        _librariesRepository = librariesRepository;
        _booksRepository = booksRepository;
        _userService = userService;
        _authenticationService = authenticationService;
        _barcodeService = barcodeService;
        _blobStorageService = blobStorageService;
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;
        _readerActionService = readerActionService;
        _logger = logger;
    }

    public async Task<ReaderDto?> GetReaderByIdAsync(int readerId)
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
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

        return entity.ToReaderDto();
    }

    public async Task<ReaderDto?> GetReaderByIdWithoutUserContextAsync(int readerId)
    {
        var entity = await _readersRepository.GetByIdWithoutLibraryAsync(readerId);
        if (entity == null)
        {
            return null;
        }

        return entity.ToReaderDto();
    }

    public async Task<ReaderDto?> GetReaderByEanAsync(string ean, Guid libraryId)
    {
        var entity = await _readersRepository.GetByEanAsync(ean, libraryId);
        if (entity == null)
        {
            return null;
        }

        return entity.ToReaderDto();
    }

    public async Task<List<ReaderDto>> GetAllReadersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchName = null
    )
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
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
        return entities.Select(e => e.ToReaderDto()).ToList();
    }

    public async Task<int> GetReadersCountAsync(string? searchName = null)
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
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

        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser == null)
        {
            throw new InvalidOperationException("Current user not found.");
        }

        if (currentUser.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        var displayName = await GetUserFullNameAsync();
        var currentLibrary = await _librariesRepository.GetByIdAsync(
            currentUser.CurrentLibraryId.Value
        );

        // Determine operation type early
        bool isCreateOperation = readerDto.ReaderId == 0;

        if (isCreateOperation)
        {

            var possibleExistedReaders = await _readersRepository.GetAllEmailsAsync(
                currentUser.CurrentLibraryId.Value
            );
            if (
                possibleExistedReaders?.Any(r =>
                    string.Equals(readerDto.Email, r, StringComparison.OrdinalIgnoreCase)
                ) == true
            )
            {
                return false;
            }

            return await CreateReaderAsync(readerDto, currentUser, currentLibrary, displayName);
        }
        else
        {
            return await UpdateReaderAsync(readerDto, currentUser, currentLibrary, displayName);
        }
    }

    private async Task<bool> CreateReaderAsync(
        ReaderDto readerDto,
        AppUser currentUser,
        Library? currentLibrary,
        string displayName
    )
    {
        // Check subscription limits before creating new reader
        var canAddReader = await _subscriptionService.CanAddReaderAsync(
            currentUser.CurrentLibraryId!.Value
        );
        if (!canAddReader)
        {
            var usage = await _subscriptionService.GetSubscriptionUsageAsync(
                currentUser.CurrentLibraryId.Value
            );
            throw new SubscriptionUpgradeRequiredException(
                $"Cannot add reader. Your current subscription plan allows a maximum of {usage.MaxReaders} readers, and you currently have {usage.CurrentReaders} readers.",
                "Additional Readers",
                "current",
                "higher tier"
            );
        }

        // Map DTO to entity
        var entity = await MapDtoToEntityAsync(readerDto);

        // Add current user's library to the reader's libraries
        if (
            currentLibrary != null
            && !entity.Libraries.Any(l => l.LibraryId == currentLibrary.LibraryId)
        )
        {
            entity.Libraries.Add(currentLibrary);
        }

        // Set audit fields for creation
        entity.CreatedBy = displayName;
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = displayName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

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
                await _readersRepository.UpdateBarcodeImageUrlAsync(entity.ReaderId, barcodeUrl);
            }

            // Update reader with EAN using UpdateEanAsync
            await _readersRepository.UpdateEanAsync(entity.ReaderId, entity.Ean);
        }

        // Log audit action for reader creation
        await _auditLogService.LogActionAsync(
            "Add",
            "Reader",
            currentUser.CurrentLibraryId!.Value,
            currentUser.Id,
            displayName,
            entity.ReaderId.ToString(),
            $"{entity.FirstName} {entity.LastName}",
            $"Added new reader with EAN: {entity.Ean}"
        );

        _logger.LogInformation(
            "Created reader {ReaderId} with EAN {Ean}",
            entity.ReaderId,
            entity.Ean
        );
        return false; // Created
    }

    private async Task<bool> UpdateReaderAsync(
        ReaderDto readerDto,
        AppUser currentUser,
        Library? currentLibrary,
        string displayName
    )
    {
        // Map DTO to entity
        var entity = await MapDtoToEntityAsync(readerDto);

        // Add current user's library to the reader's libraries
        if (
            currentLibrary != null
            && !entity.Libraries.Any(l => l.LibraryId == currentLibrary.LibraryId)
        )
        {
            entity.Libraries.Add(currentLibrary);
        }

        // Set audit fields for update
        entity.UpdatedBy = displayName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        // Update existing reader
        var updated = await _readersRepository.UpdateReaderAsync(
            entity,
            currentUser.CurrentLibraryId!.Value
        );
        if (updated == null)
        {
            _logger.LogWarning("Reader {ReaderId} not found for update", readerDto.ReaderId);
            return false;
        }

        // Log audit action for reader update
        await _auditLogService.LogActionAsync(
            "Edit",
            "Reader",
            currentUser.CurrentLibraryId!.Value,
            currentUser.Id,
            displayName,
            updated.ReaderId.ToString(),
            $"{updated.FirstName} {updated.LastName}",
            "Updated reader details"
        );

        _logger.LogInformation("Updated reader {ReaderId}", updated.ReaderId);
        return true; // Updated
    }

    public async Task<bool> DeleteReaderAsync(int readerId)
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user library not found.");
        }

        // Get reader details before deletion for audit log
        var reader = await _readersRepository.GetByIdAsync(
            readerId,
            currentUser.CurrentLibraryId.Value
        );

        var result = await _readersRepository.DeleteReaderAsync(
            readerId,
            currentUser.CurrentLibraryId.Value
        );
        if (result && reader != null)
        {
            // Log audit action for reader deletion
            var currentUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            await _auditLogService.LogActionAsync(
                "Remove",
                "Reader",
                currentUser.CurrentLibraryId.Value,
                currentUser.Id,
                currentUserName,
                readerId.ToString(),
                $"{reader.FirstName} {reader.LastName}",
                "Deleted reader and associated data"
            );

            _logger.LogInformation("Deleted reader {ReaderId}", readerId);
        }
        else
        {
            _logger.LogWarning("Reader {ReaderId} not found for deletion", readerId);
        }
        return result;
    }

    private async Task<Reader> MapDtoToEntityAsync(ReaderDto dto)
    {
        var entity = new Reader
        {
            ReaderId = dto.ReaderId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Zip = dto.Zip,
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
        var currentUser = await _authenticationService.GetAppUserAsync();
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

        var blobName = $"{currentLibrary.LibraryId}/{ean}.png";
        _logger.LogInformation(
            "Uploading barcode for reader {ReaderId} with blob name: {BlobName}",
            readerId,
            blobName
        );

        // Generate barcode image and save it using BlobStorageService directly
        using var barcodeStream = _barcodeService.GenerateBarcodeImage(ean);
        var barcodeUrl = await _blobStorageService.UploadStreamAsync(
            barcodeStream,
            "barcodes",
            blobName,
            "image/png",
            Azure.Storage.Blobs.Models.PublicAccessType.Blob
        );

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

    public async Task<List<BorrowingHistoryDto>> GetBorrowingHistoryAsync(Guid libraryId, int readerId)
    {
        var bookInstances = await _booksRepository.GetBorrowingHistoryByReaderIdAsync(libraryId, readerId);

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
        var currentUser = await _authenticationService.GetAppUserAsync();
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

    public async Task<List<ReaderActionDto>> GetReaderActionsAsync(
        int readerId,
        int page = 1,
        int pageSize = 50
    )
    {
        return await _readerActionService.GetReaderActionsAsync(readerId, page, pageSize);
    }

    public async Task<int> GetReaderActionsCountAsync(int readerId)
    {
        return await _readerActionService.GetReaderActionsCountAsync(readerId);
    }
}
