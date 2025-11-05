using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class OnboardingServiceTests
{
    private readonly Mock<ILibrariesRepository> _librariesRepositoryMock;
    private readonly Mock<IKiosksRepository> _kiosksRepositoryMock;
    private readonly Mock<IReadersRepository> _readersRepositoryMock;
    private readonly Mock<IBooksRepository> _booksRepositoryMock;
    private readonly Mock<ILogger<OnboardingService>> _loggerMock;
    private readonly OnboardingService _onboardingService;

    public OnboardingServiceTests()
    {
        _librariesRepositoryMock = new Mock<ILibrariesRepository>();
        _kiosksRepositoryMock = new Mock<IKiosksRepository>();
        _readersRepositoryMock = new Mock<IReadersRepository>();
        _booksRepositoryMock = new Mock<IBooksRepository>();
        _loggerMock = new Mock<ILogger<OnboardingService>>();

        _onboardingService = new OnboardingService(
            _librariesRepositoryMock.Object,
            _kiosksRepositoryMock.Object,
            _readersRepositoryMock.Object,
            _booksRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasNoLibraries_ReturnsAllFalseStatus()
    {
        // Arrange
        var userId = "test-user-id";
        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library>());

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasLibrary);
        Assert.False(result.HasKiosk);
        Assert.False(result.HasReader);
        Assert.False(result.HasBook);
        Assert.Null(result.LibraryId);
        Assert.False(result.IsCompleted);
        Assert.Equal(0, result.CompletedSteps);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasLibraryOnly_ReturnsLibraryTrueOthersFalse()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var library = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { library });

        _kiosksRepositoryMock
            .Setup(r => r.GetAllByLibraryIdAsync(libraryId))
            .ReturnsAsync(new List<KioskDto>());

        _readersRepositoryMock
            .Setup(r => r.GetReaderCountByLibraryIdAsync(libraryId))
            .ReturnsAsync(0);

        _booksRepositoryMock.Setup(r => r.GetBookCountByLibraryIdAsync(libraryId)).ReturnsAsync(0);

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasLibrary);
        Assert.False(result.HasKiosk);
        Assert.False(result.HasReader);
        Assert.False(result.HasBook);
        Assert.Equal(libraryId, result.LibraryId);
        Assert.False(result.IsCompleted);
        Assert.Equal(1, result.CompletedSteps);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasLibraryAndKiosk_ReturnsCorrectStatus()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var library = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };
        var kiosk = new KioskDto
        {
            KioskId = 1,
            LibraryId = libraryId,
            Location = "Test Location",
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { library });

        _kiosksRepositoryMock
            .Setup(r => r.GetAllByLibraryIdAsync(libraryId))
            .ReturnsAsync(new List<KioskDto> { kiosk });

        _readersRepositoryMock
            .Setup(r => r.GetReaderCountByLibraryIdAsync(libraryId))
            .ReturnsAsync(0);

        _booksRepositoryMock.Setup(r => r.GetBookCountByLibraryIdAsync(libraryId)).ReturnsAsync(0);

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasLibrary);
        Assert.True(result.HasKiosk);
        Assert.False(result.HasReader);
        Assert.False(result.HasBook);
        Assert.Equal(libraryId, result.LibraryId);
        Assert.False(result.IsCompleted);
        Assert.Equal(2, result.CompletedSteps);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasLibraryKioskAndReader_ReturnsCorrectStatus()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var library = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };
        var kiosk = new KioskDto
        {
            KioskId = 1,
            LibraryId = libraryId,
            Location = "Test Location",
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { library });

        _kiosksRepositoryMock
            .Setup(r => r.GetAllByLibraryIdAsync(libraryId))
            .ReturnsAsync(new List<KioskDto> { kiosk });

        _readersRepositoryMock
            .Setup(r => r.GetReaderCountByLibraryIdAsync(libraryId))
            .ReturnsAsync(5);

        _booksRepositoryMock.Setup(r => r.GetBookCountByLibraryIdAsync(libraryId)).ReturnsAsync(0);

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasLibrary);
        Assert.True(result.HasKiosk);
        Assert.True(result.HasReader);
        Assert.False(result.HasBook);
        Assert.Equal(libraryId, result.LibraryId);
        Assert.False(result.IsCompleted);
        Assert.Equal(3, result.CompletedSteps);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasCompletedAllSteps_ReturnsCompletedStatus()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var library = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };
        var kiosk = new KioskDto
        {
            KioskId = 1,
            LibraryId = libraryId,
            Location = "Test Location",
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { library });

        _kiosksRepositoryMock
            .Setup(r => r.GetAllByLibraryIdAsync(libraryId))
            .ReturnsAsync(new List<KioskDto> { kiosk });

        _readersRepositoryMock
            .Setup(r => r.GetReaderCountByLibraryIdAsync(libraryId))
            .ReturnsAsync(10);

        _booksRepositoryMock.Setup(r => r.GetBookCountByLibraryIdAsync(libraryId)).ReturnsAsync(25);

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasLibrary);
        Assert.True(result.HasKiosk);
        Assert.True(result.HasReader);
        Assert.True(result.HasBook);
        Assert.Equal(libraryId, result.LibraryId);
        Assert.True(result.IsCompleted);
        Assert.Equal(4, result.CompletedSteps);
        Assert.Equal(4, result.TotalSteps);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasMultipleLibraries_UsesFirstLibrary()
    {
        // Arrange
        var userId = "test-user-id";
        var firstLibraryId = Guid.NewGuid();
        var secondLibraryId = Guid.NewGuid();
        var libraries = new List<Library>
        {
            new Library
            {
                LibraryId = firstLibraryId,
                Name = "First Library",
                Alias = "first-lib",
            },
            new Library
            {
                LibraryId = secondLibraryId,
                Name = "Second Library",
                Alias = "second-lib",
            },
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(libraries);

        _kiosksRepositoryMock
            .Setup(r => r.GetAllByLibraryIdAsync(firstLibraryId))
            .ReturnsAsync(new List<KioskDto>());

        _readersRepositoryMock
            .Setup(r => r.GetReaderCountByLibraryIdAsync(firstLibraryId))
            .ReturnsAsync(0);

        _booksRepositoryMock
            .Setup(r => r.GetBookCountByLibraryIdAsync(firstLibraryId))
            .ReturnsAsync(0);

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(firstLibraryId, result.LibraryId);
        _kiosksRepositoryMock.Verify(r => r.GetAllByLibraryIdAsync(firstLibraryId), Times.Once);
        _kiosksRepositoryMock.Verify(r => r.GetAllByLibraryIdAsync(secondLibraryId), Times.Never);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_UserHasMultipleKiosks_ReturnsHasKioskTrue()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var library = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };
        var kiosks = new List<KioskDto>
        {
            new KioskDto
            {
                KioskId = 1,
                LibraryId = libraryId,
                Location = "Kiosk Location 1",
            },
            new KioskDto
            {
                KioskId = 2,
                LibraryId = libraryId,
                Location = "Kiosk Location 2",
            },
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { library });

        _kiosksRepositoryMock.Setup(r => r.GetAllByLibraryIdAsync(libraryId)).ReturnsAsync(kiosks);

        _readersRepositoryMock
            .Setup(r => r.GetReaderCountByLibraryIdAsync(libraryId))
            .ReturnsAsync(0);

        _booksRepositoryMock.Setup(r => r.GetBookCountByLibraryIdAsync(libraryId)).ReturnsAsync(0);

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasKiosk);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_ExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = "test-user-id";
        var expectedException = new Exception("Database connection failed");

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await _onboardingService.GetOnboardingStatusAsync(userId)
        );

        Assert.Equal(expectedException.Message, exception.Message);
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_LibrariesRepositoryThrows_DoesNotCallOtherRepositories()
    {
        // Arrange
        var userId = "test-user-id";
        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ThrowsAsync(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _onboardingService.GetOnboardingStatusAsync(userId)
        );

        _kiosksRepositoryMock.Verify(r => r.GetAllByLibraryIdAsync(It.IsAny<Guid>()), Times.Never);
        _readersRepositoryMock.Verify(
            r => r.GetReaderCountByLibraryIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
        _booksRepositoryMock.Verify(
            r => r.GetBookCountByLibraryIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Arrange
        string? userId = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _onboardingService.GetOnboardingStatusAsync(userId!)
        );
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_WithEmptyUserId_ReturnsAllFalseStatus()
    {
        // Arrange
        var userId = string.Empty;
        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library>());

        // Act
        var result = await _onboardingService.GetOnboardingStatusAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasLibrary);
        Assert.False(result.HasKiosk);
        Assert.False(result.HasReader);
        Assert.False(result.HasBook);
    }
}
