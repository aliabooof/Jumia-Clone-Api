using Jumia_Api.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jumia_Api.Infrastructure.External_Services
{
    public class SendGridEmailService:IEmailService
    {
        
            private readonly SendGridClient _sendGridClient;
            private readonly EmailAddress _fromAddress;
            private readonly IConfiguration _configuration;
            private readonly ILogger<SendGridEmailService> _logger;

            public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
            {
                _configuration = configuration;

                _logger = logger;
                var apiKey = _configuration["SendGrid:ApiKey"];

                var SenderEmail = _configuration["SendGrid:SenderEmail"];
                var SenderName = _configuration["SendGrid:SenderName"] ?? "No-Reply";

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("SendGrid API Key is missing");
                }
                if (string.IsNullOrEmpty(SenderEmail))
                {
                    _logger.LogError("SendGrid Sender Email is missing");
                }

                _sendGridClient = new SendGridClient(apiKey);
                _fromAddress = new EmailAddress(SenderEmail, SenderName);

            }


        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                if (string.IsNullOrEmpty(email))
                {
                    new ArgumentException("Email address is missing", nameof(email));

                }
                if (string.IsNullOrEmpty(subject))
                {
                    new ArgumentException("Email subject is missing", nameof(subject));

                }
                if (string.IsNullOrEmpty(htmlMessage))
                {
                    throw new ArgumentException("Email message is missing", nameof(htmlMessage));

                }

                var toAddress = new EmailAddress(email);
                var msg = MailHelper.CreateSingleEmail(
                    from: _fromAddress,
                    to: toAddress,
                    subject: subject,
                    plainTextContent: "Please view this email in a client that supports HTML emails.",
                    htmlContent: htmlMessage);


                try
                {
                    var response = await _sendGridClient.SendEmailAsync(msg);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to send email to {email} with status code {response.StatusCode}");
                    }
                    else
                    {
                        var errorMessage = await response.Body.ReadAsStringAsync().ConfigureAwait(false);

                        _logger.LogError(
                            "Failed to send email to {Email}. Status Code: {StatusCode}, Reason: {ErrorMessage}",
                            email, response.StatusCode, errorMessage);
                    }
                }
                catch
                (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}", email);
                    return;

                }
            }

        public async Task SendBulkEmailAsync(IEnumerable<string> recipientEmails, string subject, string htmlContent)
        {
            if (recipientEmails == null || !recipientEmails.Any())
            {
                throw new ArgumentException("Recipient email list cannot be empty.", nameof(recipientEmails));
            }
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException("Email subject is missing for bulk email.", nameof(subject));
            }
            if (string.IsNullOrEmpty(htmlContent))
            {
                throw new ArgumentException("Email HTML content is missing for bulk email.", nameof(htmlContent));
            }

            var msg = new SendGridMessage();
            msg.SetFrom(_fromAddress);
            msg.SetSubject(subject);
            msg.AddContent(MimeType.Html, htmlContent);
            msg.AddContent(MimeType.Text, "Please view this email in a client that supports HTML emails.");

            // Add multiple recipients
            var toRecipients = recipientEmails.Select(email => new EmailAddress(email)).ToList();
            msg.AddTos(toRecipients);

            try
            {
                _logger.LogInformation($"Attempting to send bulk email to {recipientEmails.Count()} recipients with subject: {subject}");
                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Bulk email sent successfully to {recipientEmails.Count()} recipients. Status Code: {response.StatusCode}");
                }
                else
                {
                    var errorMessage = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError(
                        "Failed to send bulk email. Status Code: {StatusCode}, Reason: {ErrorMessage}",
                        response.StatusCode, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught while sending bulk email to {RecipientCount} recipients.", recipientEmails.Count());
                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, string attachmentFileName, byte[] attachmentBytes)
        {
            if (string.IsNullOrEmpty(recipientEmail))
            {
                throw new ArgumentException("Recipient email address is missing.", nameof(recipientEmail));
            }
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException("Email subject is missing for attachment email.", nameof(subject));
            }
            if (string.IsNullOrEmpty(body))
            {
                throw new ArgumentException("Email body is missing for attachment email.", nameof(body));
            }
            if (string.IsNullOrEmpty(attachmentFileName))
            {
                throw new ArgumentException("Attachment file name is missing.", nameof(attachmentFileName));
            }
            if (attachmentBytes == null || attachmentBytes.Length == 0)
            {
                throw new ArgumentException("Attachment bytes cannot be empty.", nameof(attachmentBytes));
            }

            var toAddress = new EmailAddress(recipientEmail);
            var msg = MailHelper.CreateSingleEmail(
                from: _fromAddress,
                to: toAddress,
                subject: subject,
                plainTextContent: body,
                htmlContent: body // Often, you'll send the body as HTML too, or just plain text if it's a simple report email
            );

            // Add the attachment
            // SendGrid requires attachments to be Base64 encoded.
            var fileContent = Convert.ToBase64String(attachmentBytes);
            msg.AddAttachment(attachmentFileName, fileContent);

            try
            {
                _logger.LogInformation($"Attempting to send email with attachment '{attachmentFileName}' to {recipientEmail} with subject: {subject}");
                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email with attachment sent successfully to {recipientEmail}. Status Code: {response.StatusCode}");
                }
                else
                {
                    var errorMessage = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError(
                        "Failed to send email with attachment to {Email}. Status Code: {StatusCode}, Reason: {ErrorMessage}",
                        recipientEmail, response.StatusCode, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught while sending email with attachment to {Email}", recipientEmail);
                throw;
            }
        }
    }
    }

