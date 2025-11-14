using System;
using System.Security.Cryptography;
using System.Text.Json;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class ReaderVerificationData
{
    public string Code { get; set; } = string.Empty;
    public int ReaderId { get; set; }
}

public class ReaderAuthService : IReaderAuthService
{
    private readonly IReadersRepository _readersRepository;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IAzureQueueService _mailService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReaderAuthService> _logger;
    private const int VerificationCodeExpiryMinutes = 10;

    public ReaderAuthService(
        IReadersRepository readersRepository,
        IEmailTemplateService emailTemplateService,
        IAzureQueueService mailService,
        IMemoryCache cache,
        ILogger<ReaderAuthService> logger
    )
    {
        _readersRepository = readersRepository;
        _emailTemplateService = emailTemplateService;
        _mailService = mailService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int?> SendVerificationCodeAsync(
        ReaderLoginRequestDto loginRequest,
        Guid libraryId
    )
    {
        try
        {
            // Find the reader by EAN and date of birth
            // Use library-agnostic lookup since readers can exist across libraries
            var reader = await _readersRepository.GetByEanWithoutLibraryAsync(loginRequest.Ean);

            if (reader == null)
            {
                _logger.LogWarning("Reader not found with EAN: {Ean}", loginRequest.Ean);
                return null;
            }

            // Validate date of birth
            if (reader.DateOfBirth != loginRequest.DateOfBirth)
            {
                _logger.LogWarning(
                    "Invalid date of birth for reader with EAN: {Ean}",
                    loginRequest.Ean
                );
                return null;
            }

            // Check if reader has an email
            if (string.IsNullOrWhiteSpace(reader.Email))
            {
                _logger.LogWarning(
                    "Reader with EAN {Ean} does not have an email address",
                    loginRequest.Ean
                );
                return null;
            }

            // Generate 6-digit verification code
            var verificationCode = GenerateVerificationCode();

            // Store the verification code in cache with expiry
            var cacheKey = $"ReaderVerification_{loginRequest.Ean}";
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(
                TimeSpan.FromMinutes(VerificationCodeExpiryMinutes)
            );

            _cache.Set(
                cacheKey,
                new ReaderVerificationData { Code = verificationCode, ReaderId = reader.ReaderId },
                cacheOptions
            );

            // Send verification email
            var emailModel = new ReaderVerificationEmailDto
            {
                ReaderName = $"{reader.FirstName} {reader.LastName}",
                VerificationCode = verificationCode,
            };

            var emailBody = await _emailTemplateService.RenderReaderVerificationEmailAsync(
                emailModel
            );

            var emailMessage = new EmailMessageDto
            {
                To = reader.Email,
                Subject = "Your Library Login Verification Code",
                Body = emailBody,
                Timestamp = DateTime.UtcNow,
            };

            // Serialize to JSON for queue message
            var messageJson = JsonSerializer.Serialize(
                emailMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            await _mailService.SendMessageAsync(messageJson, "emails");

            _logger.LogInformation("Verification code sent to reader {ReaderId}", reader.ReaderId);

            return reader.ReaderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending verification code for EAN: {Ean}",
                loginRequest.Ean
            );
            throw;
        }
    }

    public async Task<int?> VerifyCodeAsync(ReaderVerificationDto verificationDto)
    {
        try
        {
            var cacheKey = $"ReaderVerification_{verificationDto.Ean}";

            if (
                _cache.TryGetValue<ReaderVerificationData>(cacheKey, out var cachedData)
                && cachedData != null
            )
            {
                if (cachedData.Code == verificationDto.VerificationCode)
                {
                    _logger.LogInformation(
                        "Verification successful for reader {ReaderId}",
                        cachedData.ReaderId
                    );
                    return cachedData.ReaderId;
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid verification code for EAN: {Ean}",
                        verificationDto.Ean
                    );
                }
            }
            else
            {
                _logger.LogWarning(
                    "Verification code expired or not found for EAN: {Ean}",
                    verificationDto.Ean
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying code for EAN: {Ean}", verificationDto.Ean);
            throw;
        }
    }

    public Task ClearVerificationCodeAsync(string ean)
    {
        var cacheKey = $"ReaderVerification_{ean}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Verification code cleared for EAN: {Ean}", ean);
        return Task.CompletedTask;
    }

    private static string GenerateVerificationCode()
    {
        // Generate a secure random 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999);
        return code.ToString();
    }
}
