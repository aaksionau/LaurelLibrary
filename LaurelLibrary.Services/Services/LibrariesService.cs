using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Exceptions;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class LibrariesService : ILibrariesService
{
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<LibrariesService> _logger;

    public LibrariesService(
        ILibrariesRepository librariesRepository,
        IUserService userService,
        IAuthenticationService authenticationService,
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService,
        IBlobStorageService blobStorageService,
        ILogger<LibrariesService> logger
    )
    {
        _librariesRepository = librariesRepository;
        _userService = userService;
        _authenticationService = authenticationService;
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<Library?> GetLibraryByIdWithDetailsAsync(Guid libraryId)
    {
        return await _librariesRepository.GetByIdWithDetailsAsync(libraryId);
    }

    public async Task<bool> AddAdministratorByEmailAsync(Guid libraryId, string email)
    {
        try
        {
            // Find the user by email
            var user = await _userService.FindUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning(
                    "User with email '{Email}' not found while trying to add as administrator to library {LibraryId}",
                    email,
                    libraryId
                );
                throw new KeyNotFoundException($"User with email '{email}' not found.");
            }

            // Get the library with details to check if user is already an administrator
            var library = await _librariesRepository.GetByIdWithDetailsAsync(libraryId);
            if (library == null)
            {
                _logger.LogWarning("Library with ID {LibraryId} not found", libraryId);
                throw new KeyNotFoundException($"Library with ID {libraryId} not found.");
            }

            // Check if user is already an administrator
            if (library.Administrators.Any(a => a.Id == user.Id))
            {
                _logger.LogInformation(
                    "User '{Email}' is already an administrator of library {LibraryId}",
                    email,
                    libraryId
                );
                throw new InvalidOperationException(
                    $"User '{email}' is already an administrator of this library."
                );
            }

            // Add the administrator
            await _librariesRepository.AddAdministratorAsync(libraryId, user.Id);

            // Set the current library for the new administrator
            await _userService.SetCurrentLibraryAsync(user.Id, libraryId);

            // Get current user for audit logging
            var currentUser = await _authenticationService.GetAppUserAsync();
            var currentUserName =
                currentUser != null
                    ? $"{currentUser.FirstName} {currentUser.LastName}".Trim()
                    : "System";

            // Log audit action for adding administrator
            await _auditLogService.LogActionAsync(
                "Add",
                "Library",
                libraryId,
                currentUser?.Id ?? "system",
                currentUserName,
                libraryId.ToString(),
                library.Name,
                $"Added administrator: {user.FirstName} {user.LastName} ({email})"
            );

            _logger.LogInformation(
                "Successfully added user '{Email}' as administrator to library {LibraryId} and set as current library",
                email,
                libraryId
            );

            return true;
        }
        catch (Exception ex)
            when (ex is not KeyNotFoundException && ex is not InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Error adding administrator '{Email}' to library {LibraryId}",
                email,
                libraryId
            );
            throw;
        }
    }

    public async Task<bool> RemoveAdministratorAsync(Guid libraryId, string userId)
    {
        try
        {
            // Get the library with details
            var library = await _librariesRepository.GetByIdWithDetailsAsync(libraryId);
            if (library == null)
            {
                _logger.LogWarning("Library with ID {LibraryId} not found", libraryId);
                throw new KeyNotFoundException($"Library with ID {libraryId} not found.");
            }

            // Get current user
            var currentUser = await _authenticationService.GetAppUserAsync();

            // Prevent removing the last administrator
            if (library.Administrators.Count <= 1)
            {
                _logger.LogWarning(
                    "Attempted to remove the last administrator from library {LibraryId}",
                    libraryId
                );
                throw new InvalidOperationException(
                    "Cannot remove the last administrator from the library."
                );
            }

            // Prevent removing yourself
            if (userId == currentUser.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to remove themselves as administrator from library {LibraryId}",
                    userId,
                    libraryId
                );
                throw new InvalidOperationException(
                    "You cannot remove yourself as an administrator."
                );
            }

            // Get the user being removed
            var userToRemove = await _userService.FindUserByIdAsync(userId);

            // Remove the administrator
            await _librariesRepository.RemoveAdministratorAsync(libraryId, userId);

            // If this library was the user's current library, set it to null
            if (userToRemove != null && userToRemove.CurrentLibraryId == libraryId)
            {
                await _userService.SetCurrentLibraryAsync(userId, null);
                _logger.LogInformation(
                    "Cleared current library for user {UserId} as they were removed from library {LibraryId}",
                    userId,
                    libraryId
                );
            }

            // Log audit action for removing administrator
            if (userToRemove != null)
            {
                var currentUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                await _auditLogService.LogActionAsync(
                    "Remove",
                    "Library",
                    libraryId,
                    currentUser.Id,
                    currentUserName,
                    libraryId.ToString(),
                    library.Name,
                    $"Removed administrator: {userToRemove.FirstName} {userToRemove.LastName} ({userToRemove.Email})"
                );
            }

            _logger.LogInformation(
                "Successfully removed user {UserId} as administrator from library {LibraryId}",
                userId,
                libraryId
            );

            return true;
        }
        catch (Exception ex)
            when (ex is not KeyNotFoundException && ex is not InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Error removing administrator {UserId} from library {LibraryId}",
                userId,
                libraryId
            );
            throw;
        }
    }

    public async Task<List<LibraryDto>> GetLibrariesForUserAsync(string userId)
    {
        try
        {
            var libraries = await _librariesRepository.GetLibrariesForUserAsync(userId);
            return libraries
                .Select(l => new LibraryDto
                {
                    LibraryId = l.LibraryId.ToString(),
                    Name = l.Name,
                    Address = l.Address ?? string.Empty,
                    Logo = l.Logo,
                    Description = l.Description,
                    CheckoutDurationDays = l.CheckoutDurationDays,
                    Alias = l.Alias,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting libraries for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Library> CreateLibraryAsync(LibraryDto libraryDto, string userId)
    {
        try
        {
            // Check if alias already exists
            var existingLibraryWithAlias = await _librariesRepository.GetByAliasAsync(
                libraryDto.Alias
            );
            if (existingLibraryWithAlias != null)
            {
                throw new InvalidOperationException(
                    $"A library with alias '{libraryDto.Alias}' already exists. Please choose a different alias."
                );
            }

            // Check subscription limits before creating new library
            var canAddLibrary = await _subscriptionService.CanAddLibraryAsync(userId);
            if (!canAddLibrary)
            {
                var userLibraries = await _librariesRepository.GetLibrariesForUserAsync(userId);
                if (userLibraries.Count > 0)
                {
                    var firstLibraryId = userLibraries.First().LibraryId;
                    var subscription = await _subscriptionService.GetLibrarySubscriptionAsync(
                        firstLibraryId
                    );
                    var tierName = subscription?.Tier switch
                    {
                        Domain.Enums.SubscriptionTier.LibraryLover => "Library Lover",
                        Domain.Enums.SubscriptionTier.BibliothecaPro => "Bibliotheca Pro",
                        _ => "current",
                    };

                    throw new SubscriptionUpgradeRequiredException(
                        $"Cannot create library. Your {tierName} subscription allows only one library.",
                        "Multiple Libraries",
                        tierName,
                        "Bibliotheca Pro"
                    );
                }
            }

            // Create the library entity
            var entity = libraryDto.ToEntity(Guid.NewGuid());

            // Get the current user and add them as administrator
            var currentUser = await _authenticationService.GetAppUserAsync();
            entity.CreatedBy = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            entity.UpdatedBy = entity.CreatedBy;

            // Create the library
            var createdLibrary = await _librariesRepository.CreateAsync(entity);
            await AddAdministratorByEmailAsync(createdLibrary.LibraryId, currentUser.Email);

            // Create a free subscription for the new library if the user doesn't have one
            await _subscriptionService.CreateTrialSubscriptionAsync(createdLibrary.LibraryId);

            // Log audit action for library creation
            var currentUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            await _auditLogService.LogActionAsync(
                "Add",
                "Library",
                createdLibrary.LibraryId,
                currentUser.Id,
                currentUserName,
                createdLibrary.LibraryId.ToString(),
                createdLibrary.Name,
                "Created new library"
            );

            _logger.LogInformation(
                "Created library {LibraryId} ({LibraryName}) for user {UserId}",
                createdLibrary.LibraryId,
                createdLibrary.Name,
                userId
            );

            return createdLibrary;
        }
        catch (Exception ex)
            when (ex is not KeyNotFoundException
                && ex is not InvalidOperationException
                && ex is not SubscriptionUpgradeRequiredException
            )
        {
            _logger.LogError(ex, "Error creating library for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteLibraryAsync(Guid libraryId)
    {
        try
        {
            // Get library details before deletion for blob cleanup
            var library = await _librariesRepository.GetByIdAsync(libraryId);
            if (library == null)
            {
                _logger.LogWarning("Library with ID {LibraryId} not found", libraryId);
                return false;
            }

            var libraryAlias = library.Alias;

            // Delete the library from the database first
            await _librariesRepository.RemoveAsync(libraryId);

            // Clean up blob storage folder if library has an alias
            if (!string.IsNullOrWhiteSpace(libraryAlias))
            {
                try
                {
                    var deletedFilesCount = await _blobStorageService.DeleteFolderAsync(
                        "book-images",
                        $"{libraryAlias}"
                    );

                    _logger.LogInformation(
                        "Deleted {DeletedFilesCount} files from blob storage for library '{LibraryAlias}' (ID: {LibraryId})",
                        deletedFilesCount,
                        libraryAlias,
                        libraryId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to clean up blob storage for library '{LibraryAlias}' (ID: {LibraryId}). Library was deleted from database.",
                        libraryAlias,
                        libraryId
                    );
                    // Don't return false here - the library was successfully deleted from database
                }
            }

            _logger.LogInformation(
                "Successfully deleted library '{LibraryName}' (ID: {LibraryId})",
                library.Name,
                libraryId
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting library {LibraryId}", libraryId);
            return false;
        }
    }
}
