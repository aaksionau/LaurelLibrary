using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Interfaces;

public interface IBookImportProcessorService
{
    /// <summary>
    /// Processes a single import record, including loading ISBNs from blob storage,
    /// processing books in chunks, and updating import status.
    /// </summary>
    /// <param name="importHistory">The import history record to process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the async operation</returns>
    Task ProcessImportAsync(ImportHistory importHistory, CancellationToken cancellationToken);

    /// <summary>
    /// Loads ISBNs from blob storage for the given blob path.
    /// </summary>
    /// <param name="blobPath">The blob path containing the CSV file</param>
    /// <returns>List of ISBNs or null if failed to load</returns>
    Task<List<string>?> LoadIsbnsFromBlobAsync(string blobPath);

    /// <summary>
    /// Processes a chunk of ISBNs by fetching book data and creating/updating books.
    /// </summary>
    /// <param name="isbns">List of ISBNs to process</param>
    /// <param name="importHistory">The import history record</param>
    /// <returns>Tuple containing the number of processed items and list of failed ISBNs</returns>
    Task<(int Processed, List<string> Failed)> ProcessChunkAsync(
        List<string> isbns,
        ImportHistory importHistory
    );
}
