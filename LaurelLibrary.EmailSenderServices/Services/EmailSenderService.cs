using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.EmailSenderServices.Services;

public class EmailSenderService : IEmailSender
{
    private readonly IAzureQueueMailService _queueMailService;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(
        IAzureQueueMailService queueMailService,
        ILogger<EmailSenderService> logger
    )
    {
        _queueMailService = queueMailService;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        _logger.LogInformation(
            "SendEmailAsync called. To: {Email}, Subject: {Subject}, MessageLength: {Length}",
            email,
            subject,
            message?.Length ?? 0
        );

        try
        {
            // Create an email message object
            var emailMessage = new EmailMessageDto
            {
                To = email,
                Subject = subject,
                Body = message,
                Timestamp = DateTime.UtcNow,
            };

            // Serialize to JSON for queue message
            var messageJson = JsonSerializer.Serialize(
                emailMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            _logger.LogDebug(
                "Serialized email message to JSON. Length: {Length}",
                messageJson.Length
            );

            // Send to Azure Queue for asynchronous processing
            var success = await _queueMailService.SendMessageAsync(messageJson);

            if (success)
            {
                _logger.LogInformation(
                    "Email queued successfully. To: {Email}, Subject: {Subject}",
                    email,
                    subject
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to queue email. To: {Email}, Subject: {Subject}",
                    email,
                    subject
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error queuing email. To: {Email}, Subject: {Subject}",
                email,
                subject
            );
            throw;
        }
    }
}
