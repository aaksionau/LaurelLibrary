using System.Text.Json;
using Hangfire;
using LaurelLibrary.Jobs.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Jobs.Jobs;

public class EmailJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailJobService> _logger;

    public EmailJobService(IServiceProvider serviceProvider, ILogger<EmailJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a background job to send an email
    /// </summary>
    /// <param name="emailMessage">The email message to send</param>
    /// <returns>The Hangfire job ID</returns>
    public string EnqueueEmailJob(EmailMessageDto emailMessage)
    {
        _logger.LogInformation(
            "Enqueueing email job for recipient: {To} with subject: {Subject}",
            emailMessage.To,
            emailMessage.Subject
        );

        var jobId = BackgroundJob.Enqueue(() => ProcessEmailAsync(emailMessage));

        _logger.LogInformation(
            "Email job enqueued with ID {JobId} for recipient: {To}",
            jobId,
            emailMessage.To
        );

        return jobId;
    }

    /// <summary>
    /// Process the email sending in background (called by Hangfire)
    /// </summary>
    /// <param name="emailMessage">The email message to send</param>
    public async Task ProcessEmailAsync(EmailMessageDto emailMessage)
    {
        _logger.LogInformation(
            "Starting Hangfire job to process email to {To} with subject: {Subject}",
            emailMessage.To,
            emailMessage.Subject
        );

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mailgunService = scope.ServiceProvider.GetRequiredService<IMailgunService>();

            // Validate required fields (same logic as the original function)
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
            var success = await mailgunService.SendEmailAsync(emailMessage);

            if (success)
            {
                _logger.LogInformation("Email successfully sent to {To}", emailMessage.To);
            }
            else
            {
                _logger.LogError("Failed to send email to {To}", emailMessage.To);
                throw new Exception($"Failed to send email to {emailMessage.To}");
            }

            _logger.LogInformation(
                "Successfully completed Hangfire email job for recipient: {To}",
                emailMessage.To
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing email job for recipient: {To} with subject: {Subject}. Error: {Error}",
                emailMessage.To,
                emailMessage.Subject,
                ex.Message
            );

            // Re-throw to let Hangfire handle the failure
            throw;
        }
    }
}
