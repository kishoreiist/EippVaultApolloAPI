using EVWebApi.DTOs;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : BaseController
    {
        private readonly IEmailSender _emailSender;
        private readonly IAuditLogService _auditlogservice;
        public EmailController(IEmailSender emailSender, IAuditLogService auditLogService)
        {
            _emailSender = emailSender;
            _auditlogservice = auditLogService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.To))
                return BadRequest("To email address is required.");

            // Add CC recipients if any
            var allRecipients = new List<string> { request.To };
            if (request.Cc != null && request.Cc.Any())
                allRecipients.AddRange(request.Cc);

            try
            {
                await _emailSender.SendAsync(
                    toEmail: request.To,
                    subject: request.Subject,
                    htmlBody: request.Body,
                    attachmentFilePaths: request.AttachmentPaths,
                    ccEmails: request.Cc
                );
                var filters = request.ToFilterLog();

                await _auditlogservice.LogAsync( CurrentUserId, CurrentUsername, "Email", "Email_Send", null,null,null,filters: filters);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to send email.",
                    detail = ex.Message
                });
            }
        }
    }
}
