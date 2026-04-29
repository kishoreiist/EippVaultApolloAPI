using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.HR;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Models.HR;
using Microsoft.EntityFrameworkCore;
using Syncfusion.XlsIO.Implementation.PivotAnalysis;

namespace EVWebApi.Services
{
    public class DocAccessReqService: IDocAccessReqService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        public DocAccessReqService(AppDbContext context,IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task RequestAccessAsync(int userId, AccessRequestDto dto)
        {
            var docLink = await _context.DocumentLink
                .FirstOrDefaultAsync(x => x.Id == dto.DocLinkId);

            if (docLink == null)
                throw new BadHttpRequestException("Invalid document");

            if (docLink.AssignedTo != userId)
                throw new UnauthorizedAccessException("Unauthorized access");


            // Combined request checks
            var requests = await _context.DocumentAccessRequest
                .Where(x => x.DocLinkId == dto.DocLinkId && x.RequestedBy == userId)
                .Select(x => new { x.Status, x.CreatedAt })
                .ToListAsync();

            if (requests.Any(x => x.Status == "pending"))
                throw new Exception("Request already pending");

            if (requests.Any(x =>
                x.Status == "rejected" &&
                x.CreatedAt > DateTime.UtcNow.AddMonths(-3)))
                throw new UnauthorizedAccessException("You cannot request again within 3 months");


            var request = new DocAccessRequest
            {
                DocLinkId = dto.DocLinkId,
                DocumentId = docLink.DocumentId,
                RequestedBy = userId,
                RequestedTo = docLink.CreatedBy,
                Reason = dto.Reason,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentAccessRequest.Add(request);
            await _context.SaveChangesAsync();


            // Combined admin + document
            var emailData = await (
                from d in _context.Documents
                join u in _context.Users on docLink.CreatedBy equals u.UserId
                where d.DocumentId == docLink.DocumentId
                select new
                {
                    AdminEmail = u.Email,
                    AdminName = u.FirstName,
                    FileName = d.FileName
                }
            ).FirstOrDefaultAsync();

            var userEmail = await _context.Users
                .Where(x => x.UserId == userId)
                .Select(x => x.Email)
                .FirstOrDefaultAsync();


            await _emailSender.SendAsync(
                ReplyTo: null,
                UserName: null,
                toEmail: emailData.AdminEmail,
                subject: "Document Access Request",
                htmlBody: $@"
            <p>Dear {emailData?.AdminName ?? "Admin"},</p>

            <p>A user has requested re-access to a document.</p>

            <ul>
                <li><strong>Requested By:</strong> {userEmail}</li>
                <li><strong>File Name:</strong> {emailData?.FileName}</li>
                <li><strong>Reason:</strong> {request.Reason ?? "N/A"}</li>
                <li><strong>Requested On:</strong> {DateTime.Now:dd MMM yyyy hh:mm tt}</li>
            </ul>

            <p>Please log in to the system to take action.</p>

                <p style='margin-top:24px;'>
                 Regards,<br/>
                  <strong>Apollo EIPP Vault Team</strong>
                  </p>
        "
            );


            _context.Notifications.Add(new Notification
            {
                UserId = docLink.CreatedBy,
                Message = "User requested access to document",
                Status = "unread"
            });

            await _context.SaveChangesAsync();
        }


        public async Task HandleAccessRequestAsync(int adminId, AccessActionDto dto)
        {
            // Get request + docLink (single query)
            var requestData = await (
                from r in _context.DocumentAccessRequest
                join ddl in _context.DocumentLink on r.DocLinkId equals ddl.Id
                where r.RequestId == dto.RequestId
                select new
                {
                    Request = r,
                    DocLink = ddl
                }
            ).FirstOrDefaultAsync();

            if (requestData == null)
                throw new Exception("Request not found");

            var request = requestData.Request;
            var docLink = requestData.DocLink;

            // Validate admin
            if (request.RequestedTo != adminId)
                throw new UnauthorizedAccessException("Not allowed");

            // Only pending allowed
            if (request.Status != "pending")
                throw new Exception("Request already processed");


            // Fetch email data (single query)
            var emailData = await (
                from u in _context.Users
                join d in _context.Documents on docLink.DocumentId equals d.DocumentId
                where u.UserId == request.RequestedBy
                select new
                {
                    UserEmail = u.Email,
                    UserName = u.FirstName,
                    FileName = d.FileName
                }
            ).FirstOrDefaultAsync();


      
            //APPROVE FLOW
            
            if (dto.Action.ToLower() == "approve")
            {
                if (dto.MaxDownload == null || dto.MaxDownload <= 0)
                    throw new Exception("Max download must be provided");

                // Create NEW access row (IMPORTANT)
                var newAccess = new DocDownloadLink
                {
                    DocumentId = docLink.DocumentId,
                    AssignedTo = request.RequestedBy,
                    CreatedBy = adminId,
                    MaxDownloads = dto.MaxDownload.Value,
                    CurrentDownloads = 0,
                    ExpiryDate = DateTime.UtcNow.AddDays(30),
                    SharedBatchId = Guid.NewGuid()
                };

                _context.DocumentLink.Add(newAccess);

                // update request
                request.Status = "approved";
                request.UpdatedAt = DateTime.UtcNow;

                // Email to user
                await _emailSender.SendAsync(
                    ReplyTo: null,
                    UserName: null,
                    toEmail: emailData.UserEmail,
                    subject: "Document Access Approved",
                    htmlBody: $@"
                <p>Dear {emailData?.UserName ?? "User"},</p>

                <p>Your request for document access has been <strong>approved</strong>.</p>

                <ul>
                    <li><strong>File Name:</strong> {emailData?.FileName}</li>
                    <li><strong>Download Limit:</strong> {dto.MaxDownload}</li>
                    <li><strong>Expiry:</strong> {DateTime.UtcNow.AddDays(30):dd MMM yyyy}</li>
                </ul>

                <p>You can login and access the document via the application.</p>
                <p style='margin-top:24px;'>
                 Regards,<br/>
                  <strong>Apollo EIPP Vault Team</strong>
                  </p>
            "
                );
            }

         
            // REJECT FLOW
         
            else if (dto.Action.ToLower() == "reject")
            {
                //if (string.IsNullOrWhiteSpace(dto.Reason))
                //    throw new Exception("Reason is required for rejection");

                request.Status = "rejected";
                request.UpdatedAt = DateTime.UtcNow;

                //Email to user
                await _emailSender.SendAsync(
                    ReplyTo: null,
                    UserName: null,
                    toEmail: emailData.UserEmail,
                    subject: "Document Access Request Rejected",
                    htmlBody: $@"
                <p>Dear {emailData?.UserName ?? "User"},</p>

                <p>Your request for document access has been <strong>rejected</strong>.</p>

                <ul>
                    <li><strong>File Name:</strong> {emailData?.FileName}</li>
                    <li><strong>Reason:</strong> {dto.Reason}</li>
                </ul>

                <p>Please contact admin for more details.</p>
            "
                );
            }
            else
            {
                throw new Exception("Invalid action");
            }


            //  Notification
            _context.Notifications.Add(new Notification
            {
                UserId = request.RequestedBy,
                Message = $"Your access request has been {dto.Action}",
                Status = "unread"
            });

            await _context.SaveChangesAsync();
        }
    }
}
