# LaurelLibrary.EmailSenderServices

A .NET 9 library for handling email operations with Azure integration, providing email template rendering and Azure Queue-based message handling.

## Features

- **Email Template Rendering**: Render HTML email templates using Razor views
- **Azure Queue Integration**: Send and manage messages via Azure Storage Queues
- **Pre-built Email Templates**: Ready-to-use templates for common scenarios
- **Strongly Typed DTOs**: Type-safe data transfer objects for email content
- **Async/Await Support**: Full asynchronous operation support 

## Technologies

- .NET 9.0
- Razor Pages for template rendering
- Azure Storage Queues
- Dependency Injection ready

## Package Dependencies 

- `Azure.Storage.Queues` (v12.22.0) - Azure Storage Queue operations
- `Microsoft.AspNetCore.Identity.UI` (v9.0.9) - Identity UI components
- `Razor.Templating.Core` (v2.1.0) - Razor template rendering engine

## Project Structure

```
├── Dtos/                          # Data Transfer Objects
│   ├── EmailConfirmationDto.cs    # Email confirmation data
│   ├── EmailMessageDto.cs         # General email message data
│   ├── PasswordResetEmailDto.cs   # Password reset email data
│   └── ReaderVerificationEmailDto.cs # Reader verification email data
├── Interfaces/                    # Service interfaces
│   ├── IAzureQueueMailService.cs  # Azure Queue service contract
│   └── IEmailTemplateService.cs   # Email template service contract
├── Services/                      # Service implementations
│   ├── AzureQueueMailService.cs   # Azure Queue operations
│   ├── EmailSenderService.cs      # Email sending logic
│   └── EmailTemplateService.cs    # Template rendering service
└── Views/                         # Razor email templates
    └── Shared/
        └── EmailTemplates/
            ├── EmailConfirmation.cshtml
            ├── PasswordResetEmail.cshtml
            └── ReaderVerificationEmail.cshtml
```

## Installation

1. Clone or reference this library in your project
2. Install the required NuGet packages (see dependencies above)
3. Configure Azure Storage connection string
4. Register services in your DI container

## Usage

### Service Registration

```csharp
// In Program.cs or Startup.cs
services.AddScoped<IEmailTemplateService, EmailTemplateService>();
services.AddScoped<IAzureQueueMailService, AzureQueueMailService>();

// Configure Azure Storage connection
services.Configure<AzureStorageOptions>(options =>
{
    options.ConnectionString = "your-azure-storage-connection-string";
    options.QueueName = "email-queue";
});
```

### Email Template Rendering

```csharp
public class EmailController : ControllerBase
{
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailController(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<string> GeneratePasswordResetEmail()
    {
        var model = new PasswordResetEmailDto
        {
            // Set your model properties
        };

        return await _emailTemplateService.RenderPasswordResetEmailAsync(model);
    }
}
```

### Azure Queue Operations

```csharp
public class EmailService
{
    private readonly IAzureQueueMailService _queueService;

    public EmailService(IAzureQueueMailService queueService)
    {
        _queueService = queueService;
    }

    public async Task<bool> QueueEmailMessage(EmailMessageDto email)
    {
        var message = JsonSerializer.Serialize(email);
        return await _queueService.SendMessageAsync(message);
    }

    public async Task<SendReceipt> QueueEmailWithOptions(EmailMessageDto email)
    {
        var message = JsonSerializer.Serialize(email);
        return await _queueService.SendMessageAsync(
            message,
            visibilityTimeout: TimeSpan.FromMinutes(5),
            timeToLive: TimeSpan.FromDays(1)
        );
    }
}
```

## Available Email Templates

### 1. Email Confirmation
Use `EmailConfirmationDto` with the email confirmation template for user email verification.

### 2. Password Reset
Use `PasswordResetEmailDto` with the password reset template for password recovery flows.

### 3. Reader Verification
Use `ReaderVerificationEmailDto` with the reader verification template for reader approval processes.

### 4. Custom Templates
Use the generic `RenderTemplateAsync<T>` method for custom email templates.

## Configuration

### Azure Storage Configuration

Ensure your `appsettings.json` includes Azure Storage configuration:

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "QueueName": "email-queue"
  }
}
```

## Error Handling

The library provides async operations with proper exception handling. Ensure you implement appropriate try-catch blocks and logging in your consuming applications.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions, please create an issue in the repository or contact the development team.

## Version History

- **v1.0.0** - Initial release with basic email templating and Azure Queue integration
