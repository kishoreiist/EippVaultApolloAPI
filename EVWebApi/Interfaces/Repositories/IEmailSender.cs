using EVWebApi.DTOs;
using EVWebApi.DTOs.Document;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IEmailSender
    {
        Task SendAsync(
                string toEmail,
               string? ReplyTo,
               string? UserName,
                string subject,
                string htmlBody,
                IEnumerable<string>? ccEmails = null,
                IEnumerable<string>? attachmentFilePaths = null,
                CancellationToken ct = default);


        Task<BatchResponseDTO> SendDocumentLinkEmailAsync(LinkEmailDto dto);
    }
}
