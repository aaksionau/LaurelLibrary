using LaurelLibrary.Jobs.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;

namespace LaurelLibrary.Jobs.Services;

public class MailgunService : IMailgunService
{
    private readonly ILogger<MailgunService> _logger;
    private readonly IConfiguration _configuration;
    private readonly RestClient _client;

    public MailgunService(ILogger<MailgunService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var domain = _configuration["Mailgun:Domain"];
        var apiKey = _configuration["Mailgun:ApiKey"];

        if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Mailgun configuration is missing. Please set Mailgun:Domain and Mailgun:ApiKey in configuration."
            );
        }

        var options = new RestClientOptions($"https://api.mailgun.net/v3/{domain}")
        {
            Authenticator = new HttpBasicAuthenticator("api", apiKey),
        };

        _client = new RestClient(options);
    }

    public async Task<bool> SendEmailAsync(EmailMessageDto emailMessage)
    {
        try
        {
            var request = new RestRequest("messages", Method.Post);
            request.AlwaysMultipartFormData = true;
            // Add form parameters for Mailgun API
            request.AddParameter(
                "from",
                _configuration["Mailgun:FromEmail"] ?? "DoNotReply@mylibrarian.org"
            );
            request.AddParameter("to", emailMessage.To);
            request.AddParameter("subject", emailMessage.Subject);
            request.AddParameter("html", emailMessage.Body);

            _logger.LogInformation(
                "Sending email to {To} with subject: {Subject}",
                emailMessage.To,
                emailMessage.Subject
            );

            var response = await _client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                _logger.LogInformation(
                    "Email sent successfully to {To}. Response: {Response}",
                    emailMessage.To,
                    response.Content
                );
                return true;
            }
            else
            {
                _logger.LogError(
                    "Failed to send email to {To}. Status: {StatusCode}, Error: {ErrorMessage}",
                    emailMessage.To,
                    response.StatusCode,
                    response.ErrorMessage
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {To}", emailMessage.To);
            return false;
        }
    }
}
