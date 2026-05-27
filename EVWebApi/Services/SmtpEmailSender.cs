
using EVWebApi.DTOs;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net;
using System.Net.Mail;


namespace EVWebApi.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly IUserRepository _userRepo;
        private readonly IGroupRepository _groupRepo;
        private readonly IDocumentRepository _docRepo;
        private readonly IConfigurationRepository _configRepo;
        private readonly IUnitOfWork _uow;

        private readonly string _pdfviewurl;
        private readonly string _userdisplay;
        public SmtpEmailSender(IOptions<EmailSettings> options, IUserRepository userRepo, IGroupRepository groupRepo, IDocumentRepository docRepo
            ,IUnitOfWork uow, IConfigurationRepository configRepo, IConfiguration config)
        { 
            _settings = options.Value; 
            _userRepo = userRepo;
            _groupRepo = groupRepo;
            _docRepo = docRepo;
            _configRepo = configRepo;
            _uow = uow;
            _pdfviewurl = config["PDFViewFEUrl:BaseUrl"];
            
        }

        public async Task<bool> SendAsync(
            string toEmail,
            string? ReplyTo,
            string? UserName,
            string subject,
            string htmlBody,
            IEnumerable<string>? ccEmails = null,
            IEnumerable<string>? attachmentFilePaths = null,
            CancellationToken ct = default)
        {
            using var message = new MailMessage();

            MailAddress replyto;
            MailAddress from;
            if (!string.IsNullOrWhiteSpace(ReplyTo))
            {
                replyto = new MailAddress(ReplyTo, UserName);
                from = new MailAddress(_settings.From, UserName);
            }
            else
            {
                from = new MailAddress(_settings.From, _settings.DisplayName);
                replyto = from;

            }

            message.From = from;
            message.ReplyToList.Add(replyto);
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
            try
            {
                await Task.Run(() => smtp.Send(message), ct);
                return true;
            }
            catch (Exception ex)
            {
                
                return false;
            }
        }


        public async Task<BatchResponseDTO> SendDocumentLinkEmailAsync(LinkEmailDto dto, int CurrentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.ReplyTo))
                throw new ArgumentNullException(nameof(dto.ReplyTo), "Sender email cannot be null or empty.");

            var user = await _userRepo.GetByEmailAsync(dto.ReplyTo);
            if (user == null) throw new NotFoundException($"User with '{dto.ReplyTo}' email not found");

            if (!dto.GroupId.HasValue && string.IsNullOrWhiteSpace(dto.To))
                throw new Exception("Group or To address is required");

            if (dto.AttachmentIds == null || !dto.AttachmentIds.Any())
                throw new Exception("No documents selected");

            var recipients = new List<EmailGroupUserDto>();
            var response = new BatchResponseDTO();
            if (dto.GroupId.HasValue)
            {
                var users = await _groupRepo.GetUsersByEmailGroupIdAsync(dto.GroupId.Value);

                if (!users.Any())
                    throw new Exception("No users found in this group");

                recipients.AddRange(users);
            }
            if (!string.IsNullOrWhiteSpace(dto.To))
            {
                var to = await _userRepo.GetByEmailAsync(dto.To);
                if (to == null)
                    throw new NotFoundException($"User with '{dto.To}' email not found");
                recipients.Add(new EmailGroupUserDto
                {
                    UserId = to.UserId,
                    Email = dto.To
                });
            }

            if (dto.Cc != null)
            {
                foreach (var cc in dto.Cc.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var ccUser = await _userRepo.GetByEmailAsync(cc);
                    if (ccUser != null)
                        recipients.Add(new EmailGroupUserDto
                        {
                            UserId = ccUser.UserId,
                            Email = ccUser.Email
                        });
                    else
                    {
                        response.TotalProcessed++;
                        response.Failed++;
                        response.FailedDocDetails.Add($"User with '{cc}' email not found");
                        continue;
                    }
                }
            }

            if (!recipients.Any())
                throw new Exception("No recipients provided.");

          
            var batchId = Guid.NewGuid();
            List<int> allAssignedDocIds = dto.AttachmentIds
                .Where(a=>a.Source== DocumentSourceType.Document)
                .Select(a => a.Id)
                .ToList();
            List<int> allOnboardingDocIds = dto.AttachmentIds
                .Where(a => a.Source == DocumentSourceType.Onboarding)
                .Select(a => a.Id)
                .ToList();
            var expiryDate = DateTime.UtcNow.AddMonths(1);
            foreach (var recipient in recipients)
            {
                response.TotalProcessed++;
                using var transaction = await _uow.BeginTransactionAsync();
                try
                {
                    var alreadyAssignedDocIds = await _docRepo.GetActiveDocumentIdsForUserAsync(recipient.UserId, allAssignedDocIds);
                    var newDocIds = allAssignedDocIds
                        .Except(alreadyAssignedDocIds)
                        .ToList();

                    var alreadyAssignedOnboardingDocIds =await _configRepo.GetActiveOnboardDocIdsForUserAsync(recipient.UserId, allOnboardingDocIds);
                    var newOnboardingDocIds = allOnboardingDocIds
                        .Except(alreadyAssignedOnboardingDocIds)
                        .ToList();

                    if (!newDocIds.Any() && !newOnboardingDocIds.Any())
                    {
                        response.Failed++;
                        response.FailedDocDetails.Add(
                            $"Email:{recipient.Email}, Error:All selected documents are already assigned.");
                        continue;
                    }
               

                    var links = newDocIds.Select(docId => new DocDownloadLink
                    {
                        DocumentId = docId,
                        AssignedTo = recipient.UserId,
                        CreatedBy=CurrentUserId,
                        SharedBatchId = batchId,
                        MaxDownloads = dto.MaxDownloads,
                        CurrentDownloads = 0,
                        ExpiryDate = expiryDate //expires after one month
                    }).ToList();


                     links.AddRange(newOnboardingDocIds.Select(docId => new DocDownloadLink
                     {
                         OnboardingDocId = docId,
                         AssignedTo = recipient.UserId,
                         CreatedBy = CurrentUserId,
                         SharedBatchId = batchId,
                         MaxDownloads = dto.MaxDownloads,
                         CurrentDownloads = 0,
                         ExpiryDate = expiryDate //expires after one month
                     }));
                    await _docRepo.CreateDocDownloadLinkAsync(links);
                    await _uow.CompleteAsync();
                    await transaction.CommitAsync();

                    var htmlBody= $"""
                        <div>
                        {dto.Body}
                        </div>

                        <!-- Secure access block -->
                        <div style="margin:50px 0; padding:15px; border:1px solid #ddd; background:#f9f9f9">
                          <p style="margin:0 0 8px 0;">
                            <strong>Secure Document Access</strong>
                          </p>
                          <p style="margin:0 0 8px 0;">
                            The documents referenced in the message are available via the secure link below:
                          </p>
                          <p>
                            <a href="{_pdfviewurl}" target="_blank">
                              Access Documents Securely
                            </a>
                          </p>
                        </div>

                         <div style="
                          margin: 150px 0 12px 0;
                          border-top: 1px solid #dcdcdc;
                          height: 1px;
                        ">
                        </div>

                        <!-- Mandatory confidentiality notice -->
                        <div style="font-size:12px; color:#555">
                          <p>
                            <strong>Confidentiality Notice:</strong>
                            This email and the documents accessible through the secure link may contain
                            confidential or sensitive information intended solely for the designated
                            recipient. Any unauthorized access, disclosure, copying, or distribution of this information is strictly prohibited and may be unlawful.
                          </p>

                          <p>
                            If you are not the intended recipient, please do not access the documents
                            and notify the sender immediately.
                          </p>
                        </div>

                        <!-- System footer -->
                        <div style="margin-top:20px; font-size:12px; color:#777">
                          <p>
                            <strong>Apollo EIPP Vault</strong><br />
                            Secure Document Management Platform
                          </p>
                        </div>
                        

                        """;

                  
                     var send = await SendAsync(
                         ReplyTo: dto.ReplyTo,
                         UserName: $"{user.FirstName} {user.LastName}",
                          toEmail: recipient.Email,
                          subject: dto.Subject,
                          htmlBody: htmlBody
                      );
                     if (send)
                     { 
                        response.Success++;

                     }
                    else
                    {

                        //await transaction.RollbackAsync();
                        response.Failed++;
                        response.FailedDocDetails.Add(
                            $"Email:{recipient.Email}, Error:Failed to send email after document assignment."
                        );
                        continue;
                    }
                                                                          
                    if (alreadyAssignedDocIds.Any() || alreadyAssignedOnboardingDocIds.Any())
                    {
                        response.FailedDocDetails.Add(
                            $"Email:{recipient.Email}, Skipped Doc ids: {string.Join(",", alreadyAssignedDocIds)},{string.Join(",", alreadyAssignedOnboardingDocIds)} (already assigned)");
                    }

                }

                catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
                {
                    await transaction.RollbackAsync();

                    response.Failed++;                     
                    response.FailedDocDetails.Add(
                        $"Email:{recipient.Email}, Error:Document is already assigned to this user and is still active."
                    );
                }

                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    response.Failed++;
                    response.FailedDocDetails.Add(
                        $"Email:{recipient.Email}, Error:Unexpected error occurred while assigning document.");
                }
            }
            return response;
        }



    }



}
