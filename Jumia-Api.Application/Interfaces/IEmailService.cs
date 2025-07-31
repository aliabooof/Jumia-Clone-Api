using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    
    
        public interface IEmailService
        {
            //Task SendSmtpAsync(string toEmail, string subject, string htmlBody);
            public Task SendEmailAsync(string to, string subject, string body);
            Task SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, string attachmentFileName, byte[] attachmentBytes);
            Task SendBulkEmailAsync(IEnumerable<string> recipientEmails, string subject, string htmlContent);
    }
    
}
