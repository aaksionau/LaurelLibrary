using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using LaurelLibrary.EmailSenderServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.EmailSenderServices.Services;

public class AzureQueueMailService : IAzureQueueMailService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<AzureQueueMailService> _logger;

    public AzureQueueMailService(
        IConfiguration configuration,
        ILogger<AzureQueueMailService> logger
    )
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("AzureStorage");
        var queueName = configuration["AzureStorage:QueueName"] ?? "emails";

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Azure Storage connection string is not configured!");
            throw new InvalidOperationException(
                "Azure Storage connection string 'AzureStorage' is not configured."
            );
        }

        _logger.LogInformation(
            "Initializing AzureQueueMailService with queue: {QueueName}",
            queueName
        );

        _queueClient = new QueueClient(connectionString, queueName);
    }

    /// <summary>
    /// Sends a message to the Azure Storage Queue
    /// </summary>
    /// <param name="message">The message content to send</param>
    /// <param name="visibilityTimeout">Optional: Time before the message becomes visible (for delayed processing)</param>
    /// <param name="timeToLive">Optional: Time the message will remain in the queue before expiring</param>
    /// <returns>SendReceipt containing message details</returns>
    public async Task<SendReceipt> SendMessageAsync(
        string message,
        TimeSpan? visibilityTimeout = null,
        TimeSpan? timeToLive = null
    )
    {
        try
        {
            _logger.LogInformation(
                "Attempting to send message to queue. Message length: {Length}",
                message?.Length ?? 0
            );

            // Ensure the queue exists
            var createResult = await _queueClient.CreateIfNotExistsAsync();
            if (createResult != null)
            {
                _logger.LogInformation("Queue '{QueueName}' was created", _queueClient.Name);
            }

            // Send the message
            var response = await _queueClient.SendMessageAsync(
                message,
                visibilityTimeout,
                timeToLive
            );

            _logger.LogInformation(
                "Message sent to queue '{QueueName}' successfully. MessageId: {MessageId}, InsertionTime: {InsertionTime}",
                _queueClient.Name,
                response.Value.MessageId,
                response.Value.InsertionTime
            );

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send message to Azure Storage Queue '{QueueName}'",
                _queueClient.Name
            );
            throw;
        }
    }

    /// <summary>
    /// Sends a message to the Azure Storage Queue (alternative method with just message)
    /// </summary>
    /// <param name="message">The message content to send</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> SendMessageAsync(string message)
    {
        try
        {
            _logger.LogDebug(
                "SendMessageAsync(simple) called with message length: {Length}",
                message?.Length ?? 0
            );
            await SendMessageAsync(message!, null, null);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessageAsync(simple) failed");
            return false;
        }
    }
}
