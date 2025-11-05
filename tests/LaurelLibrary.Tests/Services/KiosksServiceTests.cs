using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class KiosksServiceTests
    {
        private readonly Mock<IKiosksRepository> _kiosksRepositoryMock;
        private readonly Mock<IAuthenticationService> _authenticationServiceMock;
        private readonly Mock<ILogger<KiosksService>> _loggerMock;
        private readonly KiosksService _kiosksService;

        public KiosksServiceTests()
        {
            _kiosksRepositoryMock = new Mock<IKiosksRepository>();
            _authenticationServiceMock = new Mock<IAuthenticationService>();
            _loggerMock = new Mock<ILogger<KiosksService>>();

            _kiosksService = new KiosksService(
                _kiosksRepositoryMock.Object,
                _authenticationServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region GetKiosksByLibraryIdAsync Tests

        [Fact]
        public async Task GetKiosksByLibraryIdAsync_ReturnsKiosksList_WhenRepositoryReturnsData()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var expectedKiosks = new List<KioskDto>
            {
                new KioskDto
                {
                    KioskId = 1,
                    Location = "Main Entrance",
                    BrowserFingerprint = "fingerprint1",
                    LibraryId = libraryId,
                    LibraryName = "Central Library",
                },
                new KioskDto
                {
                    KioskId = 2,
                    Location = "Reading Room",
                    BrowserFingerprint = "fingerprint2",
                    LibraryId = libraryId,
                    LibraryName = "Central Library",
                },
            };

            _kiosksRepositoryMock
                .Setup(x => x.GetAllByLibraryIdAsync(libraryId))
                .ReturnsAsync(expectedKiosks);

            // Act
            var result = await _kiosksService.GetKiosksByLibraryIdAsync(libraryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(expectedKiosks, result);
            _kiosksRepositoryMock.Verify(x => x.GetAllByLibraryIdAsync(libraryId), Times.Once);
        }

        [Fact]
        public async Task GetKiosksByLibraryIdAsync_ReturnsEmptyList_WhenNoKiosksFound()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var emptyKiosks = new List<KioskDto>();

            _kiosksRepositoryMock
                .Setup(x => x.GetAllByLibraryIdAsync(libraryId))
                .ReturnsAsync(emptyKiosks);

            // Act
            var result = await _kiosksService.GetKiosksByLibraryIdAsync(libraryId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _kiosksRepositoryMock.Verify(x => x.GetAllByLibraryIdAsync(libraryId), Times.Once);
        }

        #endregion

        #region GetKioskByIdAsync Tests

        [Fact]
        public async Task GetKioskByIdAsync_ReturnsKioskDto_WhenKioskExists()
        {
            // Arrange
            var kioskId = 1;
            var libraryId = Guid.NewGuid();
            var kiosk = new Kiosk
            {
                KioskId = kioskId,
                Location = "Main Entrance",
                BrowserFingerprint = "fingerprint1",
                LibraryId = libraryId,
                Library = new Library { Name = "Central Library", Alias = "central-lib" },
            };

            _kiosksRepositoryMock.Setup(x => x.GetByIdAsync(kioskId)).ReturnsAsync(kiosk);

            // Act
            var result = await _kiosksService.GetKioskByIdAsync(kioskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(kioskId, result.KioskId);
            Assert.Equal("Main Entrance", result.Location);
            Assert.Equal("fingerprint1", result.BrowserFingerprint);
            Assert.Equal(libraryId, result.LibraryId);
            Assert.Equal("Central Library", result.LibraryName);
            _kiosksRepositoryMock.Verify(x => x.GetByIdAsync(kioskId), Times.Once);
        }

        [Fact]
        public async Task GetKioskByIdAsync_ReturnsNull_WhenKioskDoesNotExist()
        {
            // Arrange
            var kioskId = 1;

            _kiosksRepositoryMock.Setup(x => x.GetByIdAsync(kioskId)).ReturnsAsync((Kiosk?)null);

            // Act
            var result = await _kiosksService.GetKioskByIdAsync(kioskId);

            // Assert
            Assert.Null(result);
            _kiosksRepositoryMock.Verify(x => x.GetByIdAsync(kioskId), Times.Once);
        }

        [Fact]
        public async Task GetKioskByIdAsync_ReturnsKioskDto_WhenLibraryIsNull()
        {
            // Arrange
            var kioskId = 1;
            var libraryId = Guid.NewGuid();
            var kiosk = new Kiosk
            {
                KioskId = kioskId,
                Location = "Main Entrance",
                BrowserFingerprint = "fingerprint1",
                LibraryId = libraryId,
                Library = null!,
            };

            _kiosksRepositoryMock.Setup(x => x.GetByIdAsync(kioskId)).ReturnsAsync(kiosk);

            // Act
            var result = await _kiosksService.GetKioskByIdAsync(kioskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(kioskId, result.KioskId);
            Assert.Equal("Main Entrance", result.Location);
            Assert.Equal("fingerprint1", result.BrowserFingerprint);
            Assert.Equal(libraryId, result.LibraryId);
            Assert.Null(result.LibraryName);
            _kiosksRepositoryMock.Verify(x => x.GetByIdAsync(kioskId), Times.Once);
        }

        #endregion

        #region CreateOrUpdateKioskAsync Tests

        [Fact]
        public async Task CreateOrUpdateKioskAsync_CreatesNewKiosk_WhenKioskIdIsZero()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "New Location",
                BrowserFingerprint = "new-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "John",
                LastName = "Doe",
                UserName = "johndoe",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x =>
                    x.CreateAsync(
                        It.Is<Kiosk>(k =>
                            k.KioskId == 0
                            && k.Location == "New Location"
                            && k.BrowserFingerprint == "new-fingerprint"
                            && k.LibraryId == libraryId
                            && k.CreatedBy == "John Doe"
                            && k.UpdatedBy == "John Doe"
                        )
                    ),
                Times.Once
            );
            _kiosksRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Kiosk>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_UpdatesExistingKiosk_WhenKioskIdIsNotZero()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 1,
                Location = "Updated Location",
                BrowserFingerprint = "updated-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "Jane",
                LastName = "Smith",
                UserName = "janesmith",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<Kiosk>(k =>
                            k.KioskId == 1
                            && k.Location == "Updated Location"
                            && k.BrowserFingerprint == "updated-fingerprint"
                            && k.LibraryId == libraryId
                            && k.UpdatedBy == "Jane Smith"
                        )
                    ),
                Times.Once
            );
            _kiosksRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Kiosk>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_UsesUserName_WhenFirstNameAndLastNameAreEmpty()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "",
                LastName = "",
                UserName = "testuser",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x =>
                    x.CreateAsync(
                        It.Is<Kiosk>(k => k.CreatedBy == "testuser" && k.UpdatedBy == "testuser")
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_UsesUserName_WhenFirstNameAndLastNameAreWhitespace()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "   ",
                LastName = "   ",
                UserName = "testuser",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x =>
                    x.CreateAsync(
                        It.Is<Kiosk>(k => k.CreatedBy == "testuser" && k.UpdatedBy == "testuser")
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_UsesEmptyString_WhenUserNameIsNull()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "",
                LastName = "",
                UserName = null,
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x =>
                    x.CreateAsync(
                        It.Is<Kiosk>(k =>
                            k.CreatedBy == string.Empty && k.UpdatedBy == string.Empty
                        )
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_HandlesPartialNames_WhenOnlyFirstNameProvided()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "John",
                LastName = "",
                UserName = "john",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x =>
                    x.CreateAsync(
                        It.Is<Kiosk>(k => k.CreatedBy == "John" && k.UpdatedBy == "John")
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_HandlesPartialNames_WhenOnlyLastNameProvided()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "",
                LastName = "Doe",
                UserName = "doe",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x => x.CreateAsync(It.Is<Kiosk>(k => k.CreatedBy == "Doe" && k.UpdatedBy == "Doe")),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_ReturnsFalse_WhenExceptionOccurs()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            _authenticationServiceMock
                .Setup(x => x.GetAppUserAsync())
                .ThrowsAsync(new Exception("Authentication failed"));

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.False(result);
            _kiosksRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Kiosk>()), Times.Never);
            _kiosksRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Kiosk>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_ReturnsFalse_WhenRepositoryThrowsException()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "John",
                LastName = "Doe",
                UserName = "johndoe",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region DeleteKioskAsync Tests

        [Fact]
        public async Task DeleteKioskAsync_ReturnsTrue_WhenDeletionSucceeds()
        {
            // Arrange
            var kioskId = 1;

            _kiosksRepositoryMock.Setup(x => x.RemoveAsync(kioskId)).Returns(Task.CompletedTask);

            // Act
            var result = await _kiosksService.DeleteKioskAsync(kioskId);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(x => x.RemoveAsync(kioskId), Times.Once);
        }

        [Fact]
        public async Task DeleteKioskAsync_ReturnsFalse_WhenExceptionOccurs()
        {
            // Arrange
            var kioskId = 1;

            _kiosksRepositoryMock
                .Setup(x => x.RemoveAsync(kioskId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _kiosksService.DeleteKioskAsync(kioskId);

            // Assert
            Assert.False(result);
            _kiosksRepositoryMock.Verify(x => x.RemoveAsync(kioskId), Times.Once);
        }

        #endregion

        #region Edge Cases and Error Handling Tests

        [Fact]
        public async Task CreateOrUpdateKioskAsync_HandlesNullBrowserFingerprint()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = null,
                LibraryId = libraryId,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "John",
                LastName = "Doe",
                UserName = "johndoe",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x => x.CreateAsync(It.Is<Kiosk>(k => k.BrowserFingerprint == null)),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateKioskAsync_HandlesEmptyGuid()
        {
            // Arrange
            var kioskDto = new KioskDto
            {
                KioskId = 0,
                Location = "Test Location",
                BrowserFingerprint = "test-fingerprint",
                LibraryId = Guid.Empty,
            };

            var currentUser = new AppUser
            {
                Id = "user1",
                FirstName = "John",
                LastName = "Doe",
                UserName = "johndoe",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            _kiosksRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Kiosk>()))
                .ReturnsAsync(new Kiosk { Location = "Test" });

            // Act
            var result = await _kiosksService.CreateOrUpdateKioskAsync(kioskDto);

            // Assert
            Assert.True(result);
            _kiosksRepositoryMock.Verify(
                x => x.CreateAsync(It.Is<Kiosk>(k => k.LibraryId == Guid.Empty)),
                Times.Once
            );
        }

        #endregion
    }
}
