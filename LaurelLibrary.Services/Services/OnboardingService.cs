using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class OnboardingService : IOnboardingService
{
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IKiosksRepository _kiosksRepository;
    private readonly IReadersRepository _readersRepository;
    private readonly IBooksRepository _booksRepository;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        ILibrariesRepository librariesRepository,
        IKiosksRepository kiosksRepository,
        IReadersRepository readersRepository,
        IBooksRepository booksRepository,
        ILogger<OnboardingService> logger
    )
    {
        _librariesRepository = librariesRepository;
        _kiosksRepository = kiosksRepository;
        _readersRepository = readersRepository;
        _booksRepository = booksRepository;
        _logger = logger;
    }

    public async Task<OnboardingStatusDto> GetOnboardingStatusAsync(string userId)
    {
        try
        {
            var status = new OnboardingStatusDto();

            // Check if user has any libraries
            var userLibraries = await _librariesRepository.GetLibrariesForUserAsync(userId);
            status.HasLibrary = userLibraries.Any();

            if (status.HasLibrary)
            {
                // Get the first library to check for kiosks, readers, and books
                var firstLibrary = userLibraries.First();
                var libraryId = firstLibrary.LibraryId;
                status.LibraryId = libraryId;

                // Check if library has kiosks
                var kiosks = await _kiosksRepository.GetAllByLibraryIdAsync(libraryId);
                status.HasKiosk = kiosks.Any();

                // Check if library has readers
                var readerCount = await _readersRepository.GetReaderCountByLibraryIdAsync(
                    libraryId
                );
                status.HasReader = readerCount > 0;

                // Check if library has books
                var bookCount = await _booksRepository.GetBookCountByLibraryIdAsync(libraryId);
                status.HasBook = bookCount > 0;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding status for user {UserId}", userId);
            throw;
        }
    }
}
