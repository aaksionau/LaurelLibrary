using System;
using System.Text.Json;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary;

public class EmailQueueFunction
{
    private readonly ILogger<EmailQueueFunction> _logger;
    private readonly IMailgunService _mailgunService;

    public EmailQueueFunction(ILogger<EmailQueueFunction> logger, IMailgunService mailgunService)
    {
        _logger = logger;
        _mailgunService = mailgunService;
    }

    [Function(nameof(EmailQueueFunction))]
    public async Task Run([QueueTrigger("emails", Connection = "AzureStorage")] string messageText)
    {
        _logger.LogInformation("Processing email queue message: {messageText}", messageText);

        try
        {
            // Deserialize the queue message to EmailMessage object
            var emailMessage = JsonSerializer.Deserialize<EmailMessageDto>(
                messageText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (emailMessage == null)
            {
                _logger.LogError("Failed to deserialize queue message: {messageText}", messageText);
                return;
            }

            // Validate required fields
            if (
                string.IsNullOrEmpty(emailMessage.To)
                || string.IsNullOrEmpty(emailMessage.Subject)
                || string.IsNullOrEmpty(emailMessage.Body)
            )
            {
                _logger.LogWarning(
                    "Email message has missing required fields. To: {To}, Subject: {Subject}, Body length: {BodyLength}",
                    emailMessage.To,
                    emailMessage.Subject,
                    emailMessage.Body?.Length ?? 0
                );
                return;
            }

            _logger.LogInformation(
                "Sending email to {Email} with subject: {Subject} (timestamp: {Timestamp})",
                emailMessage.To,
                emailMessage.Subject,
                emailMessage.Timestamp
            );

            // Send email using Mailgun service
            var success = await _mailgunService.SendEmailAsync(emailMessage);

            if (success)
            {
                _logger.LogInformation("Email successfully sent to {To}", emailMessage.To);
            }
            else
            {
                _logger.LogError("Failed to send email to {To}", emailMessage.To);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse queue message as JSON: {messageText}",
                messageText
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error processing email queue message: {messageText}",
                messageText
            );
        }
    }
}
