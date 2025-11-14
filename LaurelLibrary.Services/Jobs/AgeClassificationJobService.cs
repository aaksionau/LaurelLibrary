using Hangfire;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Jobs;

public class AgeClassificationJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgeClassificationJobService> _logger;

    public AgeClassificationJobService(
        IServiceProvider serviceProvider,
        ILogger<AgeClassificationJobService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a background job to classify a book's appropriate age
    /// </summary>
    /// <param name="bookDto">The book information for age classification</param>
    /// <returns>The Hangfire job ID</returns>
    public string EnqueueAgeClassificationJob(LaurelBookDto bookDto)
    {
        _logger.LogInformation(
            "Enqueueing age classification job for Book {BookId}: {Title}",
            bookDto.BookId,
            bookDto.Title
        );

        var jobId = BackgroundJob.Enqueue(() => ProcessAgeClassificationAsync(bookDto));

        _logger.LogInformation(
            "Age classification job enqueued with ID {JobId} for Book {BookId}: {Title}",
            jobId,
            bookDto.BookId,
            bookDto.Title
        );

        return jobId;
    }

    /// <summary>
    /// Process the age classification in background (called by Hangfire)
    /// </summary>
    /// <param name="bookDto">The book information to classify</param>
    public async Task ProcessAgeClassificationAsync(LaurelBookDto bookDto)
    {
        _logger.LogInformation(
            "Starting Hangfire job to process age classification for Book {BookId}: {Title}",
            bookDto.BookId,
            bookDto.Title
        );

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ageClassificationService =
                scope.ServiceProvider.GetRequiredService<IAgeClassificationService>();

            // Convert LaurelBookDto to AgeClassificationBookDto
            var ageClassificationBookDto = new AgeClassificationBookDto
            {
                BookId = bookDto.BookId,
                Title = bookDto.Title ?? string.Empty,
                Description = bookDto.Synopsis ?? string.Empty,
            };

            // Classify and update the book
            var success = await ageClassificationService.ClassifyAndUpdateBookAsync(
                ageClassificationBookDto,
                CancellationToken.None
            );

            if (!success)
            {
                _logger.LogWarning(
                    "Failed to classify and update book {BookId}: {Title}",
                    bookDto.BookId,
                    bookDto.Title
                );
            }
            else
            {
                _logger.LogInformation(
                    "Successfully completed Hangfire age classification job for Book {BookId}: {Title}",
                    bookDto.BookId,
                    bookDto.Title
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing age classification for Book {BookId}: {Title} in Hangfire job: {Error}",
                bookDto.BookId,
                bookDto.Title,
                ex.Message
            );

            // Re-throw to let Hangfire handle the failure
            throw;
        }
    }
}
