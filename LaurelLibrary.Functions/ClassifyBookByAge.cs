using System;
using System.Text;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace LaurelLibrary;

public class ClassifyBookByAge
{
    private readonly ILogger<ClassifyBookByAge> _logger;
    private readonly Kernel _kernel;
    private readonly IBooksRepository _booksRepository;

    public ClassifyBookByAge(
        ILogger<ClassifyBookByAge> logger,
        Kernel kernel,
        IBooksRepository booksRepository
    )
    {
        _logger = logger;
        _kernel = kernel;
        _booksRepository = booksRepository;
    }

    [Function(nameof(ClassifyBookByAge))]
    public async Task Run(
        [QueueTrigger("age-classification-books", Connection = "AzureStorage")]
            QueueMessage message,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "C# Queue trigger function processed: {messageText}",
            message.MessageText
        );
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var book = JsonSerializer.Deserialize<AgeClassificationBookDto>(
            message.MessageText,
            jsonOptions
        );
        var ageClassificationService = _kernel.GetRequiredService<IChatCompletionService>();
        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine(
            "You are an expert book classifier. Given a book title and description, classify it into an appropriate age range."
        );
        systemPrompt.AppendLine(
            "Provide only the required JSON response as specified in the system instructions."
        );

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("Classify the following book by age:");
        userPrompt.AppendLine("Book Title:");
        userPrompt.AppendLine(book.Title);
        userPrompt.AppendLine("Book Description:");
        userPrompt.AppendLine(book.Description);

        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt.ToString());
        history.AddUserMessage(userPrompt.ToString());
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = 80,
            Temperature = 0.1,
            ResponseFormat = typeof(AgeClassificationResult),
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var chatMessage = await ageClassificationService.GetChatMessageContentAsync(
            history,
            settings,
            this._kernel,
            cancellationToken
        );

        history.Add(chatMessage);
        var classificationResult = JsonSerializer.Deserialize<AgeClassificationResult>(
            chatMessage.Content,
            jsonOptions
        );

        int.TryParse(classificationResult?.MinimalAge, out var parsedMinAge);
        int.TryParse(classificationResult?.MaximalAge, out var parsedMaxAge);

        if (classificationResult != null)
        {
            await _booksRepository.UpdateAppropriateAgeBookAsync(
                book.BookId,
                parsedMinAge,
                parsedMaxAge,
                classificationResult.Reasoning
            );
        }
        else
        {
            _logger.LogWarning("Failed to deserialize age classification result.");
        }
    }
}
