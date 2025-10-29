using System;
using System.Collections.Generic;

namespace LaurelLibrary.Services.Abstractions.Dtos;

/// <summary>
/// Message sent to Azure Storage Queue for processing ISBN imports in chunks.
/// </summary>
public class IsbnImportQueueMessage
{
    /// <summary>
    /// The ImportHistory ID to track this import.
    /// </summary>
    public Guid ImportHistoryId { get; set; }

    /// <summary>
    /// ISBNs in this chunk to be processed.
    /// </summary>
    public required List<string> Isbns { get; set; }

    /// <summary>
    /// Current chunk number (1-based).
    /// </summary>
    public int ChunkNumber { get; set; }

    /// <summary>
    /// Total number of chunks for this import.
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Total number of ISBNs in the entire import (before chunking).
    /// </summary>
    public int TotalIsbns { get; set; }

    /// <summary>
    /// Number of ISBNs remaining to be processed (after this chunk).
    /// </summary>
    public int RemainingIsbns { get; set; }
}
