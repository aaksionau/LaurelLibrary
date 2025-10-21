using Azure.Storage.Queues.Models;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IAzureQueueMailService
{
    /// <summary>
    /// Sends a message to the Azure Storage Queue
    /// </summary>
    /// <param name="message">The message content to send</param>
    /// <param name="visibilityTimeout">Optional: Time before the message becomes visible (for delayed processing)</param>
    /// <param name="timeToLive">Optional: Time the message will remain in the queue before expiring</param>
    /// <returns>SendReceipt containing message details</returns>
    Task<SendReceipt> SendMessageAsync(
        string message,
        TimeSpan? visibilityTimeout = null,
        TimeSpan? timeToLive = null
    );

    /// <summary>
    /// Sends a message to the Azure Storage Queue (alternative method with just message)
    /// </summary>
    /// <param name="message">The message content to send</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SendMessageAsync(string message);
}
