using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
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

    public AzureQueueService(IConfiguration configuration, ILogger<AzureQueueService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException(
                "AzureStorage connection string is not configured."
            );
        _logger = logger;
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
}
