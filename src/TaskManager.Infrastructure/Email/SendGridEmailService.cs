using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using TaskManager.Core.Interfaces;

namespace TaskManager.Infrastructure.Email
{
    public class SendGridEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendInvitationEmailAsync(string toEmail, string inviteLink)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("SendGrid ApiKey is missing. Cannot send invitation to {Email}.", toEmail);
                return;
            }

            if (string.IsNullOrEmpty(fromEmail) || !fromEmail.Contains('@'))
            {
                throw new ArgumentException("SendGrid:FromEmail configuration is missing or invalid.");
            }

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, "TaskManager");
            var subject = "You've been invited to TaskManager";
            var to = new EmailAddress(toEmail);

            // HTML-encode the link text and escape the URL for the href to prevent XSS
            var encodedLinkText = WebUtility.HtmlEncode(inviteLink);
            var escapedHref = WebUtility.HtmlEncode(inviteLink);
            var plainTextContent = $"You have been invited to join TaskManager. Click here to accept: {inviteLink}";
            var htmlContent = $"<p>You have been invited to join TaskManager. <a href='{escapedHref}'>Click here to accept</a></p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Invitation email sent successfully to {Email}.", toEmail);
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send invitation email to {Email}. Status: {StatusCode}. Body: {Body}", toEmail, response.StatusCode, body);
                    throw new InvalidOperationException($"SendGrid returned {response.StatusCode} when sending invitation to {toEmail}.");
                }
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our own exception
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email to {Email}.", toEmail);
                throw;
            }
        }
    }
}
