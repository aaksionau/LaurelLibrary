using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IBookImportService
{
    /// <summary>
    /// Import books from CSV file containing ISBNs.
    /// Returns the ImportHistory record with statistics.
    /// </summary>
    Task<ImportHistory> ImportBooksFromCsvAsync(Stream csvStream, string fileName);

    /// <summary>
    /// Get import history for the current user's library.
    /// </summary>
    Task<List<ImportHistory>> GetImportHistoryAsync();

    /// <summary>
    /// Get a specific import history record by ID.
    /// </summary>
    Task<ImportHistory?> GetImportHistoryByIdAsync(Guid importHistoryId);
}
