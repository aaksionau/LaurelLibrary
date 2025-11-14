using System.Text;
using System.Text.Json;
using LaurelLibrary.Jobs.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace LaurelLibrary.Jobs.Services;

public class AgeClassificationService : IAgeClassificationService
{
    private readonly ILogger<AgeClassificationService> _logger;
    private readonly Kernel _kernel;
    private readonly IBooksRepository _booksRepository;

    public AgeClassificationService(
        ILogger<AgeClassificationService> logger,
        Kernel kernel,
        IBooksRepository booksRepository
    )
    {
        _logger = logger;
        _kernel = kernel;
        _booksRepository = booksRepository;
    }

    public async Task<bool> ClassifyAndUpdateBookAsync(
        AgeClassificationBookDto book,
        CancellationToken cancellationToken = default
    )
    {
        var classificationResult = await ClassifyBookAsync(book, cancellationToken);

        if (classificationResult != null)
        {
            try
            {
                int.TryParse(classificationResult.MinimalAge, out var parsedMinAge);
                int.TryParse(classificationResult.MaximalAge, out var parsedMaxAge);

                await _booksRepository.UpdateAppropriateAgeBookAsync(
                    book.BookId,
                    parsedMinAge,
                    parsedMaxAge,
                    classificationResult.Reasoning
                );

                _logger.LogInformation(
                    "Successfully updated book {BookId} with age classification {MinAge}-{MaxAge}",
                    book.BookId,
                    parsedMinAge,
                    parsedMaxAge
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while updating book {BookId} with age classification",
                    book.BookId
                );
                return false;
            }
        }

        _logger.LogWarning("Failed to classify book {BookId}: {Title}", book.BookId, book.Title);
        return false;
    }

    public async Task<AgeClassificationResult?> ClassifyBookAsync(
        AgeClassificationBookDto book,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Starting age classification for book {BookId}: {Title}",
                book.BookId,
                book.Title
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
                _kernel,
                cancellationToken
            );

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classificationResult = JsonSerializer.Deserialize<AgeClassificationResult>(
                chatMessage.Content,
                jsonOptions
            );

            if (classificationResult != null)
            {
                _logger.LogInformation(
                    "Successfully classified book {BookId} with age range {MinAge}-{MaxAge}",
                    book.BookId,
                    classificationResult.MinimalAge,
                    classificationResult.MaximalAge
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to deserialize age classification result for book {BookId}",
                    book.BookId
                );
            }

            return classificationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while classifying book {BookId}: {Title}",
                book.BookId,
                book.Title
            );
            return null;
        }
    }
}
