namespace EVWebApi.Interfaces.Repositories
{
    public interface IEmailSender
    {
        Task SendAsync(
                string toEmail,
                string subject,
                string htmlBody,
                IEnumerable<string>? ccEmails = null,
                IEnumerable<string>? attachmentFilePaths = null,
                CancellationToken ct = default);
    }
}
