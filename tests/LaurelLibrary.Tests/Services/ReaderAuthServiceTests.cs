using System;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class ReaderAuthServiceTests
    {
        private readonly Mock<IReadersRepository> _readersRepositoryMock;
        private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<ILogger<ReaderAuthService>> _loggerMock;
        private readonly ReaderAuthService _readerAuthService;

        public ReaderAuthServiceTests()
        {
            _readersRepositoryMock = new Mock<IReadersRepository>();
            _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
            _emailSenderMock = new Mock<IEmailSender>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<ReaderAuthService>>();

            _readerAuthService = new ReaderAuthService(
                _readersRepositoryMock.Object,
                _emailTemplateServiceMock.Object,
                _emailSenderMock.Object,
                _memoryCacheMock.Object,
                _loggerMock.Object
            );
        }

        #region SendVerificationCodeAsync Tests

        [Fact]
        public async Task SendVerificationCodeAsync_ReturnsReaderId_WhenValidReaderAndEmailSent()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();
            var readerId = 123;
            var email = "test@example.com";

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            var reader = new Reader
            {
                ReaderId = readerId,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = dateOfBirth,
                Email = email,
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
            };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync(reader);

            _emailTemplateServiceMock
                .Setup(e =>
                    e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>())
                )
                .ReturnsAsync("Email body content");

            _emailSenderMock.Setup(q =>
                q.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            );

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(c => c.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock.Object);

            // Act
            var result = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                libraryId
            );

            // Assert
            Assert.Equal(readerId, result);
            _readersRepositoryMock.Verify(r => r.GetByEanWithoutLibraryAsync(ean), Times.Once);
            _emailTemplateServiceMock.Verify(
                e => e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>()),
                Times.Once
            );
            _emailSenderMock.Verify(
                q => q.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ReturnsNull_WhenReaderNotFound()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                libraryId
            );

            // Assert
            Assert.Null(result);
            _readersRepositoryMock.Verify(r => r.GetByEanWithoutLibraryAsync(ean), Times.Once);
            _emailTemplateServiceMock.Verify(
                e => e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ReturnsNull_WhenDateOfBirthMismatch()
        {
            // Arrange
            var ean = "1234567890123";
            var loginDateOfBirth = new DateOnly(1990, 1, 1);
            var readerDateOfBirth = new DateOnly(1985, 5, 15);
            var libraryId = Guid.NewGuid();

            var loginRequest = new ReaderLoginRequestDto
            {
                Ean = ean,
                DateOfBirth = loginDateOfBirth,
            };

            var reader = new Reader
            {
                ReaderId = 123,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = readerDateOfBirth,
                Email = "test@example.com",
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
            };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync(reader);

            // Act
            var result = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                libraryId
            );

            // Assert
            Assert.Null(result);
            _readersRepositoryMock.Verify(r => r.GetByEanWithoutLibraryAsync(ean), Times.Once);
            _emailTemplateServiceMock.Verify(
                e => e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ReturnsNull_WhenReaderHasNoEmail()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            var reader = new Reader
            {
                ReaderId = 123,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = dateOfBirth,
                Email = "", // Empty email
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
            };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync(reader);

            // Act
            var result = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                libraryId
            );

            // Assert
            Assert.Null(result);
            _readersRepositoryMock.Verify(r => r.GetByEanWithoutLibraryAsync(ean), Times.Once);
            _emailTemplateServiceMock.Verify(
                e => e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ReturnsNull_WhenReaderHasWhitespaceEmail()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            var reader = new Reader
            {
                ReaderId = 123,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = dateOfBirth,
                Email = "   ", // Whitespace email
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
            };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync(reader);

            // Act
            var result = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                libraryId
            );

            // Assert
            Assert.Null(result);
            _readersRepositoryMock.Verify(r => r.GetByEanWithoutLibraryAsync(ean), Times.Once);
            _emailTemplateServiceMock.Verify(
                e => e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ThrowsException_WhenRepositoryThrows()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _readerAuthService.SendVerificationCodeAsync(loginRequest, libraryId)
            );
        }

        [Fact]
        public async Task SendVerificationCodeAsync_PassesCorrectEmailModel_WhenSendingEmail()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();
            var readerId = 123;
            var firstName = "John";
            var lastName = "Doe";
            var email = "test@example.com";

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            var reader = new Reader
            {
                ReaderId = readerId,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Email = email,
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
            };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync(reader);

            _emailTemplateServiceMock
                .Setup(e =>
                    e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>())
                )
                .ReturnsAsync("Email body content");

            _emailSenderMock.Setup(q =>
                q.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            );

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(c => c.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock.Object);

            // Act
            await _readerAuthService.SendVerificationCodeAsync(loginRequest, libraryId);

            // Assert
            _emailTemplateServiceMock.Verify(
                e =>
                    e.RenderReaderVerificationEmailAsync(
                        It.Is<ReaderVerificationEmailDto>(dto =>
                            dto.ReaderName == $"{firstName} {lastName}"
                            && dto.VerificationCode.Length == 6
                            && IsValidSixDigitCode(dto.VerificationCode)
                        )
                    ),
                Times.Once
            );
        }

        private static bool IsValidSixDigitCode(string code)
        {
            return int.TryParse(code, out _);
        }

        #endregion

        #region VerifyCodeAsync Tests

        [Fact]
        public async Task VerifyCodeAsync_ReturnsReaderId_WhenCodeIsValid()
        {
            // Arrange
            var ean = "1234567890123";
            var verificationCode = "123456";
            var readerId = 123;
            var cacheKey = $"ReaderVerification_{ean}";

            var verificationDto = new ReaderVerificationDto
            {
                Ean = ean,
                VerificationCode = verificationCode,
            };

            var cachedData = new ReaderVerificationData
            {
                Code = verificationCode,
                ReaderId = readerId,
            };

            object? outValue = cachedData;
            _memoryCacheMock.Setup(c => c.TryGetValue(cacheKey, out outValue)).Returns(true);

            // Act
            var result = await _readerAuthService.VerifyCodeAsync(verificationDto);

            // Assert
            Assert.Equal(readerId, result);
            _memoryCacheMock.Verify(c => c.TryGetValue(cacheKey, out outValue), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_ReturnsNull_WhenCodeIsInvalid()
        {
            // Arrange
            var ean = "1234567890123";
            var verificationCode = "123456";
            var wrongCode = "654321";
            var readerId = 123;
            var cacheKey = $"ReaderVerification_{ean}";

            var verificationDto = new ReaderVerificationDto
            {
                Ean = ean,
                VerificationCode = wrongCode,
            };

            var cachedData = new ReaderVerificationData
            {
                Code = verificationCode, // Different from provided code
                ReaderId = readerId,
            };

            object? outValue = cachedData;
            _memoryCacheMock.Setup(c => c.TryGetValue(cacheKey, out outValue)).Returns(true);

            // Act
            var result = await _readerAuthService.VerifyCodeAsync(verificationDto);

            // Assert
            Assert.Null(result);
            _memoryCacheMock.Verify(c => c.TryGetValue(cacheKey, out outValue), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_ReturnsNull_WhenCodeNotFoundInCache()
        {
            // Arrange
            var ean = "1234567890123";
            var verificationCode = "123456";
            var cacheKey = $"ReaderVerification_{ean}";

            var verificationDto = new ReaderVerificationDto
            {
                Ean = ean,
                VerificationCode = verificationCode,
            };

            object? outValue = null;
            _memoryCacheMock.Setup(c => c.TryGetValue(cacheKey, out outValue)).Returns(false);

            // Act
            var result = await _readerAuthService.VerifyCodeAsync(verificationDto);

            // Assert
            Assert.Null(result);
            _memoryCacheMock.Verify(c => c.TryGetValue(cacheKey, out outValue), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_ReturnsNull_WhenCachedDataIsNull()
        {
            // Arrange
            var ean = "1234567890123";
            var verificationCode = "123456";
            var cacheKey = $"ReaderVerification_{ean}";

            var verificationDto = new ReaderVerificationDto
            {
                Ean = ean,
                VerificationCode = verificationCode,
            };

            object? outValue = null; // Cached data is null
            _memoryCacheMock.Setup(c => c.TryGetValue(cacheKey, out outValue)).Returns(true);

            // Act
            var result = await _readerAuthService.VerifyCodeAsync(verificationDto);

            // Assert
            Assert.Null(result);
            _memoryCacheMock.Verify(c => c.TryGetValue(cacheKey, out outValue), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_ThrowsException_WhenCacheThrows()
        {
            // Arrange
            var ean = "1234567890123";
            var verificationCode = "123456";
            var cacheKey = $"ReaderVerification_{ean}";

            var verificationDto = new ReaderVerificationDto
            {
                Ean = ean,
                VerificationCode = verificationCode,
            };

            object? outValue;
            _memoryCacheMock
                .Setup(c => c.TryGetValue(cacheKey, out outValue))
                .Throws(new Exception("Cache error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _readerAuthService.VerifyCodeAsync(verificationDto)
            );
        }

        #endregion

        #region ClearVerificationCodeAsync Tests

        [Fact]
        public async Task ClearVerificationCodeAsync_RemovesCodeFromCache()
        {
            // Arrange
            var ean = "1234567890123";
            var cacheKey = $"ReaderVerification_{ean}";

            // Act
            await _readerAuthService.ClearVerificationCodeAsync(ean);

            // Assert
            _memoryCacheMock.Verify(c => c.Remove(cacheKey), Times.Once);
        }

        [Fact]
        public async Task ClearVerificationCodeAsync_ThrowsException_WhenCacheRemoveFails()
        {
            // Arrange
            var ean = "1234567890123";
            var cacheKey = $"ReaderVerification_{ean}";

            _memoryCacheMock.Setup(c => c.Remove(cacheKey)).Throws(new Exception("Cache error"));

            // Act & Assert - Should throw
            await Assert.ThrowsAsync<Exception>(() =>
                _readerAuthService.ClearVerificationCodeAsync(ean)
            );

            _memoryCacheMock.Verify(c => c.Remove(cacheKey), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task SendVerificationCodeAndVerify_ReturnsCorrectReaderId_WhenFlowIsValid()
        {
            // Arrange
            var ean = "1234567890123";
            var dateOfBirth = new DateOnly(1990, 1, 1);
            var libraryId = Guid.NewGuid();
            var readerId = 123;
            var email = "test@example.com";

            var loginRequest = new ReaderLoginRequestDto { Ean = ean, DateOfBirth = dateOfBirth };

            var reader = new Reader
            {
                ReaderId = readerId,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = dateOfBirth,
                Email = email,
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
            };

            _readersRepositoryMock
                .Setup(r => r.GetByEanWithoutLibraryAsync(ean))
                .ReturnsAsync(reader);

            _emailTemplateServiceMock
                .Setup(e =>
                    e.RenderReaderVerificationEmailAsync(It.IsAny<ReaderVerificationEmailDto>())
                )
                .ReturnsAsync("Email body content");

            _emailSenderMock.Setup(q =>
                q.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            );

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(c => c.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock.Object);

            // Capture the verification code that gets stored in cache
            string? storedVerificationCode = null;
            cacheEntryMock
                .SetupSet(entry => entry.Value = It.IsAny<ReaderVerificationData>())
                .Callback<object>(value =>
                {
                    if (value is ReaderVerificationData data)
                    {
                        storedVerificationCode = data.Code;
                    }
                });

            // Act - Send verification code
            var sendResult = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                libraryId
            );

            // Simulate cache lookup for verification
            var cacheKey = $"ReaderVerification_{ean}";
            var cachedData = new ReaderVerificationData
            {
                Code = storedVerificationCode!,
                ReaderId = readerId,
            };

            object? outValue = cachedData;
            _memoryCacheMock.Setup(c => c.TryGetValue(cacheKey, out outValue)).Returns(true);

            var verificationDto = new ReaderVerificationDto
            {
                Ean = ean,
                VerificationCode = storedVerificationCode!,
            };

            var verifyResult = await _readerAuthService.VerifyCodeAsync(verificationDto);

            // Assert
            Assert.Equal(readerId, sendResult);
            Assert.Equal(readerId, verifyResult);
            Assert.NotNull(storedVerificationCode);
            Assert.Equal(6, storedVerificationCode.Length);
            Assert.True(int.TryParse(storedVerificationCode, out _));
        }

        #endregion
    }
}
