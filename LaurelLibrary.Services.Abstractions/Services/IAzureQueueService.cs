using System;
using System.Threading.Tasks;
using LaurelLibrary.Services.Abstractions.Dtos;

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

    /// <summary>
    /// Sends an age classification message for a book to the age classification queue.
    /// This method checks if age classification is enabled for the library before sending.
    /// </summary>
    /// <param name="bookDto">The book data to classify</param>
    /// <param name="libraryId">The library ID to check age classification feature</param>
    /// <returns>True if the message was sent successfully or skipped due to feature being disabled, false if an error occurred</returns>
    Task<bool> SendAgeClassificationMessageAsync(LaurelBookDto bookDto, Guid libraryId);
}
