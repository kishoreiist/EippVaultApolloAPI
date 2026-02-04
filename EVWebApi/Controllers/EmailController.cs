using EVWebApi.DTOs;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : BaseController
    {
        private readonly IEmailSender _emailSender;
        private readonly IAuditLogService _auditlogservice;

        private readonly IGroupRepository _groupRepo;//--------------need to move to service
        public EmailController(IEmailSender emailSender, IAuditLogService auditLogService, IGroupRepository groupRepo)
        {
            _emailSender = emailSender;
            _auditlogservice = auditLogService;
            _groupRepo = groupRepo;
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
                    ReplyTo: null,
                    UserName:null,
                    subject: request.Subject,
                    htmlBody: request.Body,
                    attachmentFilePaths: request.AttachmentPaths,
                    ccEmails: request.Cc
                );
                var filters = request.ToFilterLog("Details - ");

                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Email", "Email Sent", null, null, null, filters: filters);
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


        [HttpPost("link")]
        public async Task<IActionResult> SendDocumentLinkEmail([FromBody] LinkEmailDto data)
        {
            if (string.IsNullOrWhiteSpace(data.To) && !data.GroupId.HasValue)
                return BadRequest("To email address is required.");

            if (data.AttachmentIds == null || !data.AttachmentIds.Any())
                return BadRequest("At least one attachment ID is required to generate document link.");
            
            try
            {
                var result = await _emailSender.SendDocumentLinkEmailAsync(data);

                //need to check result and log accordingly
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Email", "Document Link Email Sent",data.GroupName);

                if (result.Success == 0 && result.Failed == 0)
                {
                    return StatusCode(500, new
                    {
                        message = "No emails were sent.",
                        detail = "An unexpected error occurred while processing the request."
                    });
                }

                if (result.Success == 0 && result.Failed > 0)
                {
                    return UnprocessableEntity(result);

                }
                if (result.Success > 0 && result.Failed > 0)
                {
                    return StatusCode(207, result); // Partial success
                }

                return Ok(new { message = "Document link email sent successfully for all recipients.", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to send document link email.",
                    detail = ex.Message
                });


            }

        }
    }
}
