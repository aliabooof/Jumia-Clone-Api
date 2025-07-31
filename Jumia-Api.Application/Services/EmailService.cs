using Jumia_Api.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Services
{
    public class EmailService : IEmailService
    {
        public Task SendBulkEmailAsync(IEnumerable<string> recipientEmails, string subject, string htmlContent)
        {
            throw new NotImplementedException();
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            Console.WriteLine("====================================");
            //Console.WriteLine($"Sending email to {to} : {body}");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {body}");
            Console.WriteLine("====================================");
            return Task.CompletedTask;
        }

        public Task SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, string attachmentFileName, byte[] attachmentBytes)
        {
            throw new NotImplementedException();
        }
    }
}
