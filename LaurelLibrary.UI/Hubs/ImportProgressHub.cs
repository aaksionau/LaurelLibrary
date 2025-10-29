using Microsoft.AspNetCore.SignalR;

namespace LaurelLibrary.UI.Hubs;

public class ImportProgressHub : Hub
{
    /// <summary>
    /// Sends import progress update to a specific client group.
    /// </summary>
    /// <param name="importHistoryId">The import history ID</param>
    /// <param name="status">Current status</param>
    /// <param name="processedChunks">Number of processed chunks</param>
    /// <param name="totalChunks">Total number of chunks</param>
    /// <param name="successCount">Number of successful imports</param>
    /// <param name="failedCount">Number of failed imports</param>
    /// <param name="totalIsbns">Total number of ISBNs</param>
    public async Task SendImportProgress(
        string importHistoryId,
        string status,
        int processedChunks,
        int totalChunks,
        int successCount,
        int failedCount,
        int totalIsbns
    )
    {
        await Clients
            .Group(importHistoryId)
            .SendAsync(
                "ReceiveImportProgress",
                new
                {
                    importHistoryId,
                    status,
                    processedChunks,
                    totalChunks,
                    successCount,
                    failedCount,
                    totalIsbns,
                    progress = totalChunks > 0
                        ? (int)((double)processedChunks / totalChunks * 100)
                        : 0,
                }
            );
    }

    /// <summary>
    /// Allows a client to join a group to receive updates for a specific import.
    /// </summary>
    /// <param name="importHistoryId">The import history ID to subscribe to</param>
    public async Task JoinImportGroup(string importHistoryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, importHistoryId);
    }

    /// <summary>
    /// Allows a client to leave a group.
    /// </summary>
    /// <param name="importHistoryId">The import history ID to unsubscribe from</param>
    public async Task LeaveImportGroup(string importHistoryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, importHistoryId);
    }
}
