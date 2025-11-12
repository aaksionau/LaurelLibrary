using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

/// <summary>
/// Service for sending messages to Azure Storage Queues.
/// </summary>
public class AzureQueueService : IAzureQueueService
{
    private readonly string _connectionString;
    private readonly ILogger<AzureQueueService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISubscriptionService _subscriptionService;

    public AzureQueueService(
        IConfiguration configuration,
        ILogger<AzureQueueService> logger,
        ISubscriptionService subscriptionService
    )
    {
        _connectionString =
            configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException(
                "AzureStorage connection string is not configured."
            );
        _logger = logger;
        _configuration = configuration;
        _subscriptionService = subscriptionService;
    }

    /// <inheritdoc />
    public async Task<bool> SendMessageAsync(string message, string queueName)
    {
        try
        {
            var queueClient = new QueueClient(_connectionString, queueName);

            // Create the queue if it doesn't exist
            await queueClient.CreateIfNotExistsAsync();

            // Send the message
            await queueClient.SendMessageAsync(message);

            _logger.LogInformation(
                "Successfully sent message to queue '{QueueName}'. Message length: {Length}",
                queueName,
                message.Length
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to queue '{QueueName}'", queueName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendAgeClassificationMessageAsync(LaurelBookDto bookDto, Guid libraryId)
    {
        try
        {
            var ageClassificationEnabled =
                await _subscriptionService.IsAgeClassificationEnabledAsync(libraryId);
            if (!ageClassificationEnabled)
            {
                _logger.LogDebug(
                    "Age classification is disabled for library {LibraryId}, skipping message for book {BookId}",
                    libraryId,
                    bookDto.BookId
                );
                return true; // Not an error, feature is just disabled
            }

            var queueName = _configuration["AzureStorage:AgeClassificationQueueName"];
            if (string.IsNullOrEmpty(queueName))
            {
                _logger.LogDebug("AgeClassificationQueueName is not configured");
                return true; // Not an error, just not configured
            }

            var ageClassificationMessage = new AgeClassificationBookDto
            {
                BookId = bookDto.BookId,
                Title = bookDto.Title ?? string.Empty,
                Description = bookDto.Synopsis ?? string.Empty,
            };

            var messageJson = System.Text.Json.JsonSerializer.Serialize(ageClassificationMessage);
            var sent = await SendMessageAsync(messageJson, queueName);

            if (sent)
            {
                _logger.LogDebug(
                    "Sent age classification message for book: {BookId} - {Title}",
                    bookDto.BookId,
                    bookDto.Title
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send age classification message for book: {BookId}",
                    bookDto.BookId
                );
            }

            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending age classification message for book: {BookId}",
                bookDto.BookId
            );
            return false;
        }
    }
}
