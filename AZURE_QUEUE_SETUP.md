# Azure Queue Service Setup

## Overview

The `AzureQueueService` enables the application to send messages to Azure Storage Queues for asynchronous processing and message-based communication between different parts of your application.

## Features

The service provides two methods for sending messages:

1. **SendMessageAsync (with options)** - Full control over message delivery:
   - Set visibility timeout (delay before message becomes available)
   - Set time-to-live (how long message stays in queue)
   - Returns detailed SendReceipt with message metadata

2. **SendMessageAsync (simple)** - Quick message sending:
   - Just pass the message content
   - Returns boolean success/failure
   - Uses default queue settings

## Configuration

### 1. Update appsettings.json

The queue configuration has been added to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net"
  },
  "AzureStorage": {
    "BarcodeContainerName": "barcodes",
    "LibraryLogoContainerName": "library-logos",
    "QueueName": "messages"
  }
}
```

### 2. For Local Development with Azurite

You can use the same Azurite emulator that's used for blob storage:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  }
}
```

Make sure Azurite is running:
```bash
azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log
```

## Usage Examples

### Register the Service

Add to your `Program.cs` or service registration:

```csharp
builder.Services.AddScoped<AzureQueueService>();
```

### Send a Simple Message

```csharp
public class YourService
{
    private readonly AzureQueueService _queueService;
    
    public YourService(AzureQueueService queueService)
    {
        _queueService = queueService;
    }
    
    public async Task DoSomethingAsync()
    {
        // Simple send
        bool success = await _queueService.SendMessageAsync("Hello, Queue!");
        
        if (success)
        {
            // Message sent successfully
        }
    }
}
```

### Send a Message with Options

```csharp
// Send a message that becomes visible after 5 minutes
var receipt = await _queueService.SendMessageAsync(
    message: "Process this order",
    visibilityTimeout: TimeSpan.FromMinutes(5),
    timeToLive: TimeSpan.FromHours(24)
);

Console.WriteLine($"Message ID: {receipt.MessageId}");
Console.WriteLine($"Inserted at: {receipt.InsertionTime}");
```

### Common Use Cases

#### 1. Order Processing
```csharp
await _queueService.SendMessageAsync($"OrderId:{orderId}");
```

#### 2. Email Notifications
```csharp
var emailData = JsonSerializer.Serialize(new { To = email, Subject = subject, Body = body });
await _queueService.SendMessageAsync(emailData);
```

#### 3. Delayed Tasks
```csharp
// Process after 1 hour
await _queueService.SendMessageAsync(
    message: "TaskData",
    visibilityTimeout: TimeSpan.FromHours(1)
);
```

## How It Works

1. **Queue Creation**: The service automatically creates the queue if it doesn't exist
2. **Message Sending**: Messages are sent asynchronously to Azure Storage Queue
3. **Logging**: All operations are logged for monitoring and debugging
4. **Error Handling**: Exceptions are caught, logged, and re-thrown for proper error handling

## Queue Message Structure

Messages in Azure Storage Queues:
- **Maximum size**: 64 KB per message
- **Maximum TTL**: 7 days (default: infinite if not specified)
- **Visibility timeout**: 0 seconds to 7 days (default: 0)
- **Base64 encoding**: Messages are automatically Base64 encoded by the SDK

## Receiving Messages

To receive messages from the queue, you can add methods like:

```csharp
public async Task<QueueMessage[]> ReceiveMessagesAsync(int maxMessages = 10)
{
    var messages = await _queueClient.ReceiveMessagesAsync(maxMessages);
    return messages.Value;
}

public async Task DeleteMessageAsync(string messageId, string popReceipt)
{
    await _queueClient.DeleteMessageAsync(messageId, popReceipt);
}
```

## Dependencies

- **Azure.Storage.Queues** (v12.22.0): Azure Queue Storage SDK

## Best Practices

1. **Message Size**: Keep messages small (< 64 KB). For larger payloads, store data in blob storage and send the blob URL
2. **Idempotency**: Design message processors to handle duplicate messages gracefully
3. **Poison Messages**: Implement dead-letter queue handling for messages that fail processing
4. **Monitoring**: Monitor queue length and processing times in Azure Portal
5. **Security**: Use SAS tokens or managed identities instead of connection strings in production

## Troubleshooting

### Connection Issues
- Verify the connection string in appsettings.json
- For Azurite, ensure it's running on default ports (10001 for queues)
- Check network connectivity to Azure

### Message Not Appearing
- Check if visibility timeout is set (message may be invisible temporarily)
- Verify the queue name matches in sender and receiver
- Use Azure Storage Explorer to inspect queue contents

### Performance Issues
- Consider using batch operations for multiple messages
- Implement retry policies for transient failures
- Monitor queue metrics in Azure Portal
