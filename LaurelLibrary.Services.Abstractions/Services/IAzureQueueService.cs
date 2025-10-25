using System.Threading.Tasks;

namespace LaurelLibrary.Services.Abstractions.Services;

/// <summary>
/// Service for sending messages to Azure Storage Queues.
/// </summary>
public interface IAzureQueueService
{
    /// <summary>
    /// Send a message to the specified Azure Storage Queue.
    /// </summary>
    /// <param name="message">The message content to send.</param>
    /// <param name="queueName">The name of the queue to send to.</param>
    /// <returns>True if the message was sent successfully, otherwise false.</returns>
    Task<bool> SendMessageAsync(string message, string queueName);
}
