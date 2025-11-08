using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<IBooksRepository> _booksRepositoryMock;
    private readonly Mock<IReadersRepository> _readersRepositoryMock;
    private readonly Mock<ILibrariesRepository> _librariesRepositoryMock;
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly Mock<ILogger<SubscriptionService>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionServiceTests()
    {
        _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
        _booksRepositoryMock = new Mock<IBooksRepository>();
        _readersRepositoryMock = new Mock<IReadersRepository>();
        _librariesRepositoryMock = new Mock<ILibrariesRepository>();
        _stripeServiceMock = new Mock<IStripeService>();
        _loggerMock = new Mock<ILogger<SubscriptionService>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("test-webhook-secret");

        _subscriptionService = new SubscriptionService(
            _subscriptionRepositoryMock.Object,
            _booksRepositoryMock.Object,
            _readersRepositoryMock.Object,
            _librariesRepositoryMock.Object,
            _stripeServiceMock.Object,
            _loggerMock.Object,
            _httpContextAccessorMock.Object,
            _configurationMock.Object
        );
    }

    [Fact]
    public async Task CanAddLibraryAsync_UserHasNoLibraries_ReturnsTrue()
    {
        // Arrange
        var userId = "test-user-id";
        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library>());

        // Act
        var result = await _subscriptionService.CanAddLibraryAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAddLibraryAsync_FreeUserWithOneLibrary_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var existingLibrary = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };

        var subscription = new Subscription
        {
            LibraryId = libraryId,
            Tier = SubscriptionTier.LibraryLover,
            Status = SubscriptionStatus.Active,
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { existingLibrary });

        _subscriptionRepositoryMock
            .Setup(r => r.GetByLibraryIdAsync(libraryId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _subscriptionService.CanAddLibraryAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanAddLibraryAsync_LibraryLoverUserWithOneLibrary_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var existingLibrary = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };

        var subscription = new Subscription
        {
            LibraryId = libraryId,
            Tier = SubscriptionTier.LibraryLover,
            Status = SubscriptionStatus.Active,
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { existingLibrary });

        _subscriptionRepositoryMock
            .Setup(r => r.GetByLibraryIdAsync(libraryId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _subscriptionService.CanAddLibraryAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanAddLibraryAsync_ProUserWithMultipleLibraries_ReturnsTrue()
    {
        // Arrange
        var userId = "test-user-id";
        var libraryId = Guid.NewGuid();
        var existingLibrary = new Library
        {
            LibraryId = libraryId,
            Name = "Test Library",
            Alias = "test-lib",
        };

        var subscription = new Subscription
        {
            LibraryId = libraryId,
            Tier = SubscriptionTier.BibliothecaPro,
            Status = SubscriptionStatus.Active,
        };

        _librariesRepositoryMock
            .Setup(r => r.GetLibrariesForUserAsync(userId))
            .ReturnsAsync(new List<Library> { existingLibrary });

        _subscriptionRepositoryMock
            .Setup(r => r.GetByLibraryIdAsync(libraryId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _subscriptionService.CanAddLibraryAsync(userId);

        // Assert
        Assert.True(result);
    }
}
