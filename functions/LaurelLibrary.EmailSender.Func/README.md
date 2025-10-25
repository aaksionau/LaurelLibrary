# LaurelLibrary Email Sender Azure Function

This Azure Function processes email messages from an Azure Storage Queue and sends them using the Mailgun email service.

## Features

- **Queue-triggered Azure Function**: Automatically processes messages from Azure Storage Queue
- **Mailgun Integration**: Sends emails using Mailgun's REST API
- **Error Handling**: Comprehensive logging and error handling
- **JSON Message Processing**: Deserializes queue messages containing email data
- **Dependency Injection**: Uses .NET's built-in DI container

## Message Format

The function expects queue messages in the following JSON format:

```json
{
  "email": "recipient@example.com",
  "subject": "Email Subject",
  "message": "Email content goes here",
  "timestamp": "2025-10-20T10:30:00Z"
}
```

## Configuration

### Required Settings

Update your `local.settings.json` (for local development) or Azure Function App Settings (for production) with the following:

```json
{
  "Values": {
    "laurelschoolacc_STORAGE": "your-azure-storage-connection-string",
    "Mailgun:Domain": "your-mailgun-domain.com",
    "Mailgun:ApiKey": "your-mailgun-api-key",
    "Mailgun:FromEmail": "noreply@your-domain.com"
  }
}
```

### Mailgun Setup

1. Sign up for a Mailgun account at https://www.mailgun.com/
2. Verify your domain or use the sandbox domain for testing 
3. Get your API key from the Mailgun dashboard
4. Update the configuration settings above

## Local Development

### Prerequisites

- .NET 9.0 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azure Storage Account

### Running Locally

1. Clone the repository
2. Update `local.settings.json` with your configuration
3. Restore packages:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```
5. Start the function:
   ```bash
   func start
   ```

### Testing

You can test the function by adding messages to the queue using the provided `QueueTestHelper` class:

```csharp
var connectionString = "your-storage-connection-string";
var testHelper = new QueueTestHelper(connectionString);

await testHelper.AddEmailToQueueAsync(
    email: "test@example.com",
    subject: "Test Email",
    message: "This is a test message."
);
```

## Deployment

### To Azure

1. Create an Azure Function App
2. Configure the application settings with your Mailgun credentials
3. Deploy using:
   ```bash
   func azure functionapp publish your-function-app-name
   ```

### Environment Variables for Production

Set these in your Azure Function App configuration:

- `laurelschoolacc_STORAGE`: Azure Storage connection string
- `Mailgun:Domain`: Your Mailgun domain
- `Mailgun:ApiKey`: Your Mailgun API key
- `Mailgun:FromEmail`: Default sender email address

## Architecture

```
Azure Storage Queue → Azure Function → Mailgun API → Email Delivery
```

1. **Queue Message**: JSON message is added to the `emails` queue
2. **Function Trigger**: Azure Function is triggered by the queue message
3. **Deserialization**: Message is parsed into `EmailMessage` object
4. **Validation**: Required fields are validated
5. **Email Sending**: Mailgun service sends the email
6. **Logging**: Results are logged for monitoring

## Error Handling

The function includes comprehensive error handling:

- **JSON Parsing Errors**: Logged when queue message isn't valid JSON
- **Validation Errors**: Logged when required fields are missing
- **Mailgun API Errors**: Logged with response details
- **General Exceptions**: Caught and logged with full details

## Monitoring

The function uses Application Insights for monitoring and logging. Key metrics include:

- Queue processing time
- Email send success/failure rates
- Error rates and details
- Performance metrics

## Dependencies

- **Microsoft.Azure.Functions.Worker**: Azure Functions runtime
- **RestSharp**: HTTP client for Mailgun API
- **System.Text.Json**: JSON serialization
- **Azure Storage Queues**: Queue trigger binding