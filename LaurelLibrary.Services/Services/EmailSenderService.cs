using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Jobs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class EmailSenderService : IEmailSender
{
    private readonly EmailJobService _jobService;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(EmailJobService jobService, ILogger<EmailSenderService> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string message)
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

            _jobService.EnqueueEmailJob(emailMessage);

            return Task.CompletedTask;
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
