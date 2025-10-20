using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class LibrariesService : ILibrariesService
{
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IUserService _userService;
    private readonly ILogger<LibrariesService> _logger;

    public LibrariesService(
        ILibrariesRepository librariesRepository,
        IUserService userService,
        ILogger<LibrariesService> logger
    )
    {
        _librariesRepository = librariesRepository;
        _userService = userService;
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
            var currentUser = await _userService.GetAppUserAsync();

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
}
