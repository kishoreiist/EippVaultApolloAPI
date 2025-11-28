using EVWebApi.DTOs;
using EVWebApi.Interfaces.Repositories;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;


namespace EVWebApi.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> options)
            => _settings = options.Value;

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IEnumerable<string>? ccEmails = null,
            IEnumerable<string>? attachmentFilePaths = null,
            CancellationToken ct = default)
        {
            using var message = new MailMessage();
      
            var from = string.IsNullOrWhiteSpace(_settings.DisplayName)
                ? new MailAddress(_settings.From)
                : new MailAddress(_settings.From, _settings.DisplayName);

            message.From = from;
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;
            if (ccEmails != null)
            {
                foreach (var cc in ccEmails)
                    message.CC.Add(cc);
            }

            if (attachmentFilePaths != null)
            {
                foreach (var path in attachmentFilePaths)
                {
                    if (File.Exists(path))
                    {
                        message.Attachments.Add(new Attachment(path));
                    }
                }
            }

            using var smtp = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
            {
                Credentials = new NetworkCredential(_settings.Smtp.User, _settings.Smtp.Password),
                EnableSsl = _settings.Smtp.EnableSsl
            };

            // SmtpClient has no true async send; use Task.Run to avoid blocking the request thread.
            await Task.Run(() => smtp.Send(message), ct);
        }
    
    }
}
