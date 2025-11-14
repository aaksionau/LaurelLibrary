using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Jobs.Interfaces;

public interface IAgeClassificationService
{
    /// <summary>
    /// Classifies a book's appropriate age range based on its title and description using AI,
    /// and updates the book record with the classification results.
    /// </summary>
    /// <param name="book">The book information to classify</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if the classification and update were successful, false otherwise</returns>
    Task<bool> ClassifyAndUpdateBookAsync(
        AgeClassificationBookDto book,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Classifies a book's appropriate age range based on its title and description using AI.
    /// </summary>
    /// <param name="book">The book information to classify</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The age classification result or null if classification fails</returns>
    Task<AgeClassificationResult?> ClassifyBookAsync(
        AgeClassificationBookDto book,
        CancellationToken cancellationToken = default
    );
}
