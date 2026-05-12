using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.HR;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Exceptions;
using EVWebApi.Helpers.ExportToExcel;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using EVWebApi.Models;
using EVWebApi.Models.HR;
using Humanizer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Syncfusion.EJ2.Grids;
using System.Data.Common;
using System.Security;
using System.Text.RegularExpressions;

namespace EVWebApi.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IMetadataReaderFactoryService _metadataReaderFactory;
        private readonly AppDbContext _context;
        private readonly IDocumentRepository _docrepo;
        private readonly IConfigurationRepository _repo;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly string _externalUploadUrl;
        private readonly string _uploadRoot;
        private readonly string _baseUrl;
        private readonly string _storageRoot;
        private readonly string _clientName;
        private readonly NpgsqlDataSource _dataSource;
        public ConfigurationService(AppDbContext context, IDocumentRepository docrepo, IConfigurationRepository repo, IEmailSender emailSender, IConfiguration config,
            IMetadataReaderFactoryService metadataReaderFactory, INotificationService notificationService, NpgsqlDataSource dataSource)
        {
            _context = context;
            _docrepo = docrepo;
            _repo = repo;
            _emailSender = emailSender;
            _notificationService = notificationService;
            _metadataReaderFactory= metadataReaderFactory;
            _baseUrl = config["Frontend:BaseUrl"];
            _externalUploadUrl = config["DocumentSettings:ExternalUploadURL"];
            _uploadRoot = config["DocumentSettings:OnboardingFilePath"];
            _storageRoot = config["DocumentSettings:StorageRoot"];
            _clientName = config["DocumentSettings:ClientName"];
            _dataSource = dataSource;
        }

        public async Task<CollectionResponseDto> CreateCollectionAsync(CreateCollectionDto dto, int? userId)
        {

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Collection name is required");

            if (dto.DocumentTypes == null || !dto.DocumentTypes.Any())
                throw new Exception("At least one document type is required");

            var collName = await _repo.GetCollectionByNameAsync(dto.Name);
            if (collName != null)
                throw new ConflictException("Collection with same name already exists");

            var docTypeEntities = new List<DocumentTypes>();
            foreach (var type in dto.DocumentTypes)
            {
                if (string.IsNullOrWhiteSpace(type))
                    continue;

                var docType = await _docrepo.GetOrCreateDocLabelAsync(type.Trim());
                docTypeEntities.Add(docType);
            }

            var collection = new DocumentCollection
            {
                Name = dto.Name,
                Designation=dto.Designation,
                Region=dto.Region,
                Status="active",
                IsExternal=dto.IsExternal,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.Value,
                CollectionDocumentTypes = docTypeEntities.Select(dt => new CollectionDocumentType
                {
                    DocumentTypeId = dt.Id

                }).ToList()
            };

            _context.DocumentCollections.Add(collection);
            await _context.SaveChangesAsync();


            return new CollectionResponseDto
            {
                Id = collection.Id,
                Name = collection.Name,
                Region=collection.Region,
                Designation=collection.Designation,
                CreatedAt = collection.CreatedAt,
                CreatedBy = collection.CreatedBy,
                DocumentTypes = docTypeEntities.Select(x => x.Label).ToList()
            };
        }

        public async Task<CollectionResponseDto> UpdateCollectionAsync(int id, CreateCollectionDto dto, int? userId)
        {
            var collection = await _repo.GetCollectionByIdAsync(id);
            if (collection == null)
                throw new NotFoundException("Collection not found");
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Collection name is required");
            if (dto.DocumentTypes == null || !dto.DocumentTypes.Any())
                throw new Exception("At least one document type is required");
            var collName = await _repo.GetCollectionByNameAsync(dto.Name);
            if (collName != null && collName.Id != id)
                throw new ConflictException("Another collection with same name already exists");
            var docTypeEntities = new List<DocumentTypes>();
            foreach (var type in dto.DocumentTypes)
            {
                if (string.IsNullOrWhiteSpace(type))
                    continue;
                var docType = await _docrepo.GetOrCreateDocLabelAsync(type.Trim());
                docTypeEntities.Add(docType);
            }
            collection.Name = dto.Name;
            collection.Designation = dto.Designation;
            collection.Region = dto.Region;
            collection.Status = dto.Status;
            collection.IsExternal = dto.IsExternal;
            //collection.UpdatedAt = DateTime.UtcNow;
            //collection.UpdatedBy = userId.Value;
            // Update document types
            var existingDocTypeIds = collection.CollectionDocumentTypes.Select(cd => cd.DocumentTypeId).ToList();
            var newDocTypeIds = docTypeEntities.Select(dt => dt.Id).Distinct().ToList();

            var toRemoveIds = existingDocTypeIds.Except(newDocTypeIds).ToList();
            var toAddIds = newDocTypeIds.Except(existingDocTypeIds).ToList();

            var toRemove = collection.CollectionDocumentTypes
                .Where(cd => toRemoveIds.Contains(cd.DocumentTypeId))
                .ToList();

            // Remove old types not in new list
            foreach (var item in toRemove)
            {
                collection.CollectionDocumentTypes.Remove(item);
            }

            // ADD
            foreach (var idToAdd in toAddIds)
            {
                collection.CollectionDocumentTypes.Add(new CollectionDocumentType
                {
                    DocumentTypeId = idToAdd
                });
            }
            await _context.SaveChangesAsync();
            return new CollectionResponseDto
            {
                Id = collection.Id,
                Name = collection.Name,
                IsExternal=collection.IsExternal,
                Status=collection.Status,
                Designation=collection.Designation,
                Region=collection.Region,
                CreatedAt = collection.CreatedAt,
                CreatedBy = collection.CreatedBy,
                DocumentTypes = docTypeEntities.Select(x => x.Label).ToList()
            };
        }


        public async Task<PagedResponse<CollectionListResponseDto>> GetCollectionListAsync(CollectionQueryDto dto)
        {
            var query = _repo.Query();

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var normalized = dto.Name.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(normalized));
            }
            if (!string.IsNullOrWhiteSpace(dto.DocType))
            {
                var normalizedType = dto.DocType.Trim().ToLower();
                query = query.Where(c => c.CollectionDocumentTypes
                    .Any(cd => cd.DocumentType.Label.ToLower().Contains(normalizedType)));
            }
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                query = query.Where(c => c.Status.ToLower() == dto.Status.ToLower());
            }
            else
                query = query.Where(c => c.Status.ToLower() == "active");
            if (dto.IsExternal==true)
                query=query.Where(c=>c.IsExternal==true);
            else if(dto.IsExternal==false)
                query=query.Where(c=>c.IsExternal==false);
            else
                query=query.Where(c=>c.IsExternal==false || c.IsExternal==true);


            var totalItems = await query.CountAsync();
            if (dto.PageSize <= 0)
                dto.PageSize = 10;

            int totalPages = (int)Math.Ceiling(totalItems / (double)dto.PageSize);

            if (dto.PageNumber <= 0)
                dto.PageNumber = 1;

            if (dto.PageNumber > totalPages && totalPages > 0)
                dto.PageNumber = totalPages;

            var collections = await query
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();
            var collectionDtos = collections.Select(c => new CollectionListResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Region=c.Region,
                Designation=c.Designation,
                Status=c.Status,
                IsExternal=c.IsExternal,
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy,
                DocumentTypes = c.CollectionDocumentTypes.Select(cd => cd.DocumentType.Label).ToList(),
                DocTypeCount = c.CollectionDocumentTypes.Count
            }).ToList();
            return new PagedResponse<CollectionListResponseDto>
            {
                Data = collectionDtos,
                TotalRecords = totalItems,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize
            };
        }

        public async Task<List<CollectionDropDownDto>> GetCollectionDropDownListAsync(CollectionDropDownQueryDto dto)
        {
            var query = _context.DocumentCollections
                .Where(d=>d.Status.ToLower() == "active")
                .AsQueryable();

            
            if (dto.IsExternal.HasValue)
            {
                query = query.Where(d => d.IsExternal == dto.IsExternal.Value);
            }

            if(!string.IsNullOrWhiteSpace(dto.Region)) {
                query = query.Where(d => d.Region == dto.Region);
            }

            return await query
                .Select(d => new CollectionDropDownDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Region=d.Region
                })
                .ToListAsync();

        }
        public async Task<CollectionResponseDto> GetCollectionByIdAsync(int id)
        {
            var collection = await _repo.GetCollectionByIdAsync(id);
            if (collection == null)
                throw new NotFoundException("Collection not found");
            return new CollectionResponseDto
            {
                Id = collection.Id,
                Name = collection.Name,
                Region = collection.Region,
                Designation = collection.Designation,
                IsExternal = collection.IsExternal,
                Status=collection.Status,
                CreatedAt = collection.CreatedAt,
                CreatedBy = collection.CreatedBy,
                DocumentTypes = collection.CollectionDocumentTypes.Select(cd => cd.DocumentType.Label).ToList()
            };
        }
        public async Task DeleteCollectionAsync(int id)
        {
            var collection = await _repo.GetCollectionByIdAsync(id);
            if (collection == null)
                throw new NotFoundException("Collection not found");
            collection.Status = "deleted";
            //_context.DocumentCollections.Remove(collection);
            await _context.SaveChangesAsync();
        }


        public async Task<ConfigurationResponseDto> SendConfigurationAsync(ConfigurationRequestDto dto, int userId)
        {
            
            var collection = await _repo.GetCollectionByIdAsync(dto.CollectionId);
            if (collection == null)
                throw new NotFoundException("Collection not found");

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Email, u.Username })
                .FirstOrDefaultAsync();

            var request = new ConfigRequest
            {
                ConfigName=dto.Name,
                CollectionId = dto.CollectionId,
                ExpiryDate = dto.ExpiryDate.Value,
                Description = dto.Description,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            
            var recipients = dto.Emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(email => new ConfigRequestRecipient
                {
                    Email = email.Trim(),
                    Token = Guid.NewGuid().ToString(),
                    Status = "pending"
                })
                .ToList();

            request.Recipients = recipients;

      
            _context.ConfigurationRequests.Add(request);
            await _context.SaveChangesAsync();

            //Generate email tasks (parallel)

            int success = 0;
            int failed = 0;
            var failedDetails = new List<string>();
            var lockObj = new object();

            var semaphore = new SemaphoreSlim(5); // max 5 parallel
            var emailTasks = recipients.Select(async r =>
            {
                var link = $"{_externalUploadUrl}/{r.Token}";

                var body = $@"
                <p>Greetings,</p>
                <p>Please upload the required documents using the link below:</p>
                <p><a href='{link}'><b>Upload Your Documents</b></a></p>
                <p style='font-size:13px;color:#FF0000;'><i>Note : This link will expire on {dto.ExpiryDate}.</i></p>
                <br><p>If you need any assistance, feel free to contact us.</p></br>

                <p>Regards,</p>
                <p>Apollo EIPP Vault Team</p>
            ";
                var subject = "Action Required: Upload Your Documents";
                await semaphore.WaitAsync();
                try
                {
                    var sent = await _emailSender.SendAsync(r.Email, ReplyTo: user.Email, UserName:"Apollo OnBoarding", subject, body);

                    lock (lockObj)
                    {
                        if (sent)
                            success++;
                        else
                            failed++;
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        failed++;
                        failedDetails.Add($"Email failed for {r.Email}: {ex.Message}");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            // Send all emails in parallel
            await Task.WhenAll(emailTasks);


            return new ConfigurationResponseDto
            {
                RequestId = request.Id,
                TotalEmails = recipients.Count,
                Success= success,
                Failed=failed,
                FailedEmailDetails= failedDetails

            };
        }


        public async Task<UploadPageResponseDto> GetUploadDocsAsync(string token)
        {
            var recipient = await _repo.GetConfigRequestByToken(token);

            if (recipient == null)
                throw new Exception("Invalid link");

           
            if (recipient.Request.ExpiryDate < DateTime.UtcNow)
            {
                recipient.Status = "expired";
                await _context.SaveChangesAsync();
                throw new Exception("Link expired");
            }

            var uploadedDocIds = recipient.UploadedDocuments
                .Select(x => x.DocumentTypeId)
                .ToList();
            var totalDocs = recipient.Request.Collection.CollectionDocumentTypes.Count;

            var uploadedCount = uploadedDocIds.Distinct().Count();

            var statusDto = new UploadStatusDto
            {
                Status = recipient.Status,
                Total = totalDocs,
                UploadedCount = uploadedCount,
                Pending = totalDocs - uploadedCount
            };

            var documents = recipient.Request.Collection.CollectionDocumentTypes
                .Select(cd => new DocumentTypeDto
                {
                    DocumentTypeId = cd.DocumentTypeId,
                    DocType = cd.DocumentType.Label,
                    Uploaded = uploadedDocIds.Contains(cd.DocumentTypeId)
                })
                .ToList();

            return new UploadPageResponseDto
            {
                RecipientId = recipient.Id,
                Email = recipient.Email,
                Designation = recipient.Request.Collection.Designation,
                IsExternal = recipient.Request.Collection.IsExternal,
                CollectionName = recipient.Request.Collection.Name,
                Status = statusDto,
                Documents = documents
            };
        }

        public async Task<UploadResultDto> UploadDocumentsAsync(OnboardingDocsDto dto)
        {
            var recipient = await _repo.GetConfigRequestByToken(dto.Token);

            if (recipient == null)
                throw new Exception("Invalid token");

            if (!string.IsNullOrWhiteSpace(dto.Email))
                if (dto.Email != recipient.Email) throw new BadRequestException("Provided email doesn't match with the one given while applying for the job.");

            var assignedDocsId = recipient.Request.Collection.CollectionDocumentTypes.Select(i => i.DocumentTypeId);
            if (recipient.Request.ExpiryDate < DateTime.UtcNow)
            {
                recipient.Status = "expired";
                await _context.SaveChangesAsync();
                throw new BadRequestException("Link expired");
            }

            recipient.Name = dto.Name;
            recipient.Phone = dto.Phone;
            recipient.DateOfBirth = dto.Dob;
            recipient.Adhaar = dto.AdhaarNo;
            recipient.PAN = dto.PAN;
            foreach (var doc in dto.Files)
            {
                if (doc.File == null || doc.File.Length == 0)
                    continue;
                if (assignedDocsId.Contains(doc.DocumentTypeId))
                {

                    //var fileName = $"{Guid.NewGuid()}_{doc.File.FileName}";
                    var folderPath = _uploadRoot
                        .Replace("{StorageRoot}", _storageRoot)
                        .Replace("{ClientName}", _clientName);

                    var folderName = $"{dto.Name}_{dto.Token}";
                    var safecandidateName = dto.Name.Trim().Replace(" ", "_");
                    var orgfolderName = $"{safecandidateName}_{dto.Token}";

                    var finalPath = Path.Combine(folderPath, orgfolderName);

                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);

                    var originalExtension = Path.GetExtension(doc.File.FileName);

                    // safer doc type label
                    var docTypeLabel = recipient.Request.Collection.CollectionDocumentTypes
                        .First(x => x.DocumentTypeId == doc.DocumentTypeId)
                        .DocumentType.Label
                        .Trim()
                        .Replace(" ", "_");

                    // display filename
                    var displayFileName = $"{safecandidateName}_{docTypeLabel}{originalExtension}";


                    var filePath = Path.Combine(finalPath, displayFileName);
                    var dbPath = Path.Combine(orgfolderName, displayFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await doc.File.CopyToAsync(stream);
                    }

                    //Replace if already exists
                    var existing = recipient.UploadedDocuments
                        .FirstOrDefault(x => x.DocumentTypeId == doc.DocumentTypeId);

                    if (existing != null)
                    {
                        existing.FilePath = dbPath;
                        existing.UploadedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.OnboardingHRDocument.Add(new OnboardingDocument
                        {
                            RecipientId = recipient.Id,
                            DocumentTypeId = doc.DocumentTypeId,
                            FilePath = dbPath,
                            FileName = displayFileName,
                            UploadedAt = DateTime.UtcNow,
                            Status = "active",
                            Source = "candidate_upload"
                        });
                    }
                }
                else
                {
                    throw new NotFoundException("Uploaded files includes documents which is not assigned to the recipient");
                }
            }
            await _context.SaveChangesAsync();

            var totalDocs = recipient.Request.Collection.CollectionDocumentTypes.Count;

            var uploadedCount = await _repo.GetUploadCount(recipient.Id);

            if (uploadedCount == 0)
                recipient.Status = "pending";
            else if (uploadedCount < totalDocs)
                recipient.Status = "inProgress";
            else
            {
                recipient.Status = "completed";
                recipient.CompletedAt = DateTime.UtcNow;
            }
            recipient.AccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify HR after successful upload

            if (recipient.Status == "completed")
            {
                await _notificationService.CreateAsync(
                    recipient.Request.CreatedBy,
                    $"Candidate {recipient.Name} completed all document uploads"
                );

                
                var hrUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == recipient.Request.CreatedBy);

                if (hrUser != null && !string.IsNullOrEmpty(hrUser.Email))
                {
                    var subject = "Candidate Document Submission Completed";
                    var link = $"{_baseUrl}";
                    var body = $@"
                        Hello {hrUser.FirstName},

                        The candidate <b>{recipient.Name}</b> has successfully uploaded all required documents.

                        <p><b>Candidate Email:</b> {recipient.Email}</p>
                        <b>Completion Time:</b> {DateTime.UtcNow}

                        
                        <p><a href='{link}'>Please log in to review the documents.</a></p>


                        <br><p><i>This is a system-generated email. Please do not reply.</i></p></br>
                        Regards,<br/>
                        Apollo EIPP Vault Team
                     ";

                    await _emailSender.SendAsync(hrUser.Email, null,null, subject, body);
                }
            }
            else
            {
                await _notificationService.CreateAsync(
                    recipient.Request.CreatedBy,
                    $"Candidate {recipient.Name} uploaded documents, Status - In Progress"
                );
            }

            var documents = recipient.Request.Collection.CollectionDocumentTypes
                .Select(cd => new DocumentTypeDto
                {
                    DocumentTypeId = cd.DocumentTypeId,
                    DocType = cd.DocumentType.Label,
                    Uploaded = recipient.UploadedDocuments
                        .Any(u => u.DocumentTypeId == cd.DocumentTypeId)
                })
                .ToList();

            return new UploadResultDto
            {
                Status = recipient.Status,
                Documents = documents
            };


        }

        public async Task<List<ConfigListDto>> GetAllConfigsAsync(int userId,string userType, ConfigQueryParamsDto dto)
        {
            var query = _repo.GetConfigListAsync();
            if (!string.IsNullOrEmpty(userType) && userType!="super_admin")
            {
                query = query.Where(r => r.CreatedBy == userId);
            }
            if (!string.IsNullOrEmpty(dto.Region))
                query = query.Where(r => r.Collection.Region == dto.Region);
            //if(!string.IsNullOrEmpty(dto.Status))
            //    query=query.Where(r=>r.Recipients.)

            return await query
                .Select(r => new ConfigListDto
                {
                    ConfigId = r.Id,
                    CollectionName = r.Collection.Name,
                    Region=r.Collection.Region,
                    CreatedAt = r.CreatedAt,
                    Description=r.Description,
                    TotalRecipients = r.Recipients.Count,
                    Pending = r.Recipients.Count(x => x.Status == "pending"),
                    InProgress = r.Recipients.Count(x => x.Status == "inProgress"),
                    Completed = r.Recipients.Count(x => x.Status == "completed")
                })
                .ToListAsync();

           
        }



        //public async Task<List<ConfigRequestDetailsDto>> GetConfigRequestByIdAsync( ConfigQueryDetailDto dto)
        //{
        //    var request = await _repo.GetConfigRequestByIdAsync(dto?.Status);

        //    if (request == null)
        //        throw new Exception("Request not found");

        //    // All document types in collection
        //    var docTypes = request.Collection?.CollectionDocumentTypes
        //        .Select(dt => new DocumentTypeListDto
        //        {
        //            DocumentTypeId = dt.DocumentTypeId,
        //            DocType = dt.DocumentType.Label
        //        })
        //        .ToList();

        //    var recipientIds = request.Recipients.Select(r => r.Id).ToList();

        //    // Fetch uploaded docs
        //    var uploadedDocs = await _context.OnboardingHRDocument
        //        .Where(x => recipientIds.Contains(x.RecipientId))
        //        .Select(x => new
        //        {
        //            x.RecipientId,
        //            x.DocumentTypeId,
        //            x.FilePath,
        //            DocType = x.DocumentType.Label
        //        })
        //        .ToListAsync();

        //    // Build recipients
        //    var recipients = request.Recipients.Select(r =>
        //    {
        //        var userDocs = uploadedDocs
        //            .Where(x => x.RecipientId == r.Id)
        //            .ToList();


        //        // SUBMITTED

        //        var submittedDocuments = userDocs
        //            .Select(x => new DocumentTypeDetailDto
        //            {
        //                DocumentTypeId = x.DocumentTypeId,
        //                DocType = x.DocType,
        //                FilePath = x.FilePath
        //            })
        //            .ToList();

        //        var submittedDocTypeIds = submittedDocuments
        //            .Select(d => d.DocumentTypeId)
        //            .Distinct()
        //            .ToHashSet();


        //        //  PENDING

        //        var pendingDocuments = docTypes
        //            .Where(dt => !submittedDocTypeIds.Contains(dt.DocumentTypeId))
        //            .Select(dt => new DocumentTypeListDto
        //            {
        //                DocumentTypeId = dt.DocumentTypeId,
        //                DocType = dt.DocType
        //            })
        //            .ToList();


        //        // FINAL RECIPIENT

        //        return new RecipientDto
        //        {
        //            RecipientId = r.Id,
        //            Email = r.Email,
        //            Name = r.Name,
        //            PAN = r.PAN,
        //            Adhaar = r.Adhaar,
        //            Dob=r.DateOfBirth,
        //            Status = r.Status,
        //            CompletedAt = r.CompletedAt,

        //            Submitted = new SubmittedDocDto
        //            {
        //                TotalSubmittedCount = submittedDocuments.Count,
        //                Documents = submittedDocuments
        //            },

        //            Pending = new PendingDocDto
        //            {
        //                TotalPendingCount = pendingDocuments.Count,
        //                Documents = pendingDocuments
        //            }
        //        };
        //    }).ToList();

        //    // Final response
        //    return new ConfigRequestDetailsDto
        //    {
        //        ConfigId = request.Id,
        //        CollectionName = request.Collection?.Name,
        //        Description = request.Description,
        //        CreatedAt = request.CreatedAt,
        //        TotalDocs = docTypes.Count,
        //        Recipients = recipients
        //    };
        //}
        public async Task<List<ConfigRequestDetailsDto>> GetConfigRequestsAsync(ConfigQueryDetailDto dto)
        {

            var requests = await _repo.GetConfigRequestAsync(dto);

            if (requests == null || !requests.Any())
                return new List<ConfigRequestDetailsDto>();

            var result = new List<ConfigRequestDetailsDto>();

            var allRecipientIds = requests
                .SelectMany(r => r.Recipients)
                .Select(r => r.Id)
                .ToList();

            var allUploadedDocs = await _context.OnboardingHRDocument
                .Where(x => allRecipientIds.Contains(x.RecipientId))
                .Select(x => new
                {
                    x.RecipientId,
                    x.DocumentTypeId,
                    x.Id,
                    x.FilePath,
                    DocType = x.DocumentType.Label
                })
                .ToListAsync();

            foreach (var request in requests)
            {
                var docTypes = request.Collection?.CollectionDocumentTypes
                    .Select(dt => new DocumentTypeListDto
                    {
                        DocumentTypeId = dt.DocumentTypeId,
                        DocType = dt.DocumentType.Label
                    })
                    .ToList() ?? new List<DocumentTypeListDto>();

                var recipientIds = request.Recipients.Select(r => r.Id).ToList();

                var uploadedDocs = allUploadedDocs
                    .Where(x => recipientIds.Contains(x.RecipientId))
                    .ToList();

                var recipients = request.Recipients.Select(r =>
                {
                    var userDocs = uploadedDocs
                        .Where(x => x.RecipientId == r.Id)
                        .ToList();

                    var submittedDocuments = userDocs
                        .Select(x => new DocumentTypeDetailDto
                        {
                            DocumentTypeId = x.DocumentTypeId,
                            DocType = x.DocType,
                            FileId=x.Id,
                            FilePath = x.FilePath
                        })
                        .ToList();

                    var submittedDocTypeIds = submittedDocuments
                        .Select(d => d.DocumentTypeId)
                        .ToHashSet();

                    var pendingDocuments = docTypes
                        .Where(dt => !submittedDocTypeIds.Contains(dt.DocumentTypeId))
                        .Select(dt => new DocumentTypeListDto
                        {
                            DocumentTypeId = dt.DocumentTypeId,
                            DocType = dt.DocType
                        })
                        .ToList();

                    return new RecipientDto
                    {
                        RecipientId = r.Id,
                        Email = r.Email,
                        Name = r.Name,
                        PAN = r.PAN,
                        Adhaar = r.Adhaar,
                        Dob = r.DateOfBirth,
                        Status = r.Status,
                        CompletedAt = r.CompletedAt,
                        IsHired = r.IsHired,
                        Submitted = new SubmittedDocDto
                        {
                            TotalSubmittedCount = submittedDocuments.Count,
                            Documents = submittedDocuments
                        },

                        Pending = new PendingDocDto
                        {
                            TotalPendingCount = pendingDocuments.Count,
                            Documents = pendingDocuments
                        }
                    };
                }).ToList();

                result.Add(new ConfigRequestDetailsDto
                {
                    ConfigId = request.Id,
                    ConfigName= request.ConfigName,
                    CollectionName = request.Collection?.Name,
                    Region= request.Collection?.Region,
                    Description = request.Description,
                    CreatedAt = request.CreatedAt,
                    TotalDocs = docTypes.Count,
                    Recipients = recipients
                });
            }

            return result;
        }


        public async Task<DocumentStreamResultDTO?> GetOnboardingDocumentStream(int id)
        {
            var doc = await _repo.GetOnboardingFilesAsync(id);
            if (doc == null)
                throw new NotFoundException("Document not found");

            var relativePath = doc.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());
            string fileName = Path.GetFileName(relativePath);
            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var fullPath = Path.Combine(uploadRootTemplate, relativePath);

            if (!fullPath.StartsWith(_storageRoot))
                throw new SecurityException("Invalid file path");

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true
            );

            return new DocumentStreamResultDTO
            {
                Stream = stream,
                FilePath = fullPath,
                FileName = fileName
            };
        }


        //-----------------------------------------HR Onboarding Confirmation Batch Upload------------------------------

        public async Task<HrUploadResponseDto> OnboardingExcelUploadAsync(OnboardingUploadDto dto, int? userId)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new ValidationException("File is required");

            var extension = Path.GetExtension(dto.File.FileName)
                .ToLower();

            var reader = _metadataReaderFactory.GetReader(extension);

            var parseResult =
                await reader.ReadAsync<HrParsedRowDto>(dto.File);

            if (!parseResult.Records.Any())
                throw new ValidationException("No records found");


            // validate headers
            ValidateHeaders(parseResult.Headers);

            // create batch
            var batch = new HrConfirmationBatch
            {
                FileName = dto.File.FileName,
                UploadedBy = userId.Value,
                UploadedAt = DateTime.UtcNow,
                Status = "pending"
            };

            await _repo.CreateOnboardingBatch(batch);
            await _context.SaveChangesAsync();
            var response = new HrUploadResponseDto();

            int rowNumber = 1;

            foreach (var row in parseResult.Records)
            {
                var batchRow = new HrConfirmationBatchRow
                {
                    BatchId = batch.BatchId,
                    RowNumber = rowNumber,
                    EmployeeId = row.EmployeeId,
                    Designation = row.Designation,
                    DOJ = row.DOJ,
                    CandidateName = row.Name,
                    Email = row.Email,
                    Phone = row.Phone,
                    PAN = row.PAN,
                    Aadhaar = row.Aadhaar,
                    DOB = row.DOB
                };

                try
                {
                    // row validation
                    ValidateRow(row);

                    // candidate matching
                    var candidate =
                        await _repo.MatchOnboardingCandidateAsync(row);

                    if (candidate == null)
                    {
                        batchRow.Status = "candidate_not_found";
                        batchRow.ErrorMessage =
                            "No matching candidate found";
                    }
                   
                    else
                    {
                        batchRow.CandidateId = candidate.Id;

                        var alreadyConfirmed =
                        await _context.HrConfirmationBatchRows
                            .AnyAsync(x =>
                            x.CandidateId == candidate.Id 
                           && x.IsConfirmed 
                            );
                        var existingPendingRows = await _context.HrConfirmationBatchRows
                            .Where(x =>
                                x.CandidateId == candidate.Id &&
                                x.IsConfirmed != true &&
                                x.Status == "matched")
                            .ToListAsync();


                        if (alreadyConfirmed)
                        {
                            batchRow.Status = "already_confirmed";
                            batchRow.ErrorMessage =
                                "Candidate is already hired";
                        }

                        else
                        {
                            foreach (var oldRow in existingPendingRows)
                            {
                                oldRow.Status = "superseded";
                            }
                            batchRow.Status = "matched";
                        }

                    }
                }
                catch (Exception ex)
                {
                    batchRow.Status = "validation_failed";
                    batchRow.ErrorMessage = ex.Message;
                }

                

                await _repo.CreateOnboardingBatchRows(batchRow);

                response.Records.Add(new RowResponseDto
                {
                    RowNumber = rowNumber,
                    CandidateId = batchRow.CandidateId,
                    CandidateName = row.Name,
                    EmployeeId = row.EmployeeId,
                    Status = batchRow.Status,
                    ErrorMessage = batchRow.ErrorMessage
                });

                rowNumber++;
            }

            await _context.SaveChangesAsync();


            response.BatchId = batch.BatchId;
            response.TotalRows = response.Records.Count;
            response.SuccessCount =
                response.Records.Count(x => x.Status == "matched");

            response.FailureCount =
                response.Records.Count(x => x.Status != "matched");

            batch.TotalRows = response.TotalRows;
            batch.SuccessCount = response.SuccessCount;
            batch.FailureCount = response.FailureCount;

            await _context.SaveChangesAsync();

            return response;
        }


        //----------export to excel failed report
        public async Task<(byte[], string)> ExportFailedRowsAsync(int batchId)
        {
            var failedRows = await _context.HrConfirmationBatchRows
                .Where(x =>
                    x.BatchId == batchId &&
                    x.Status != "matched")
                .AsNoTracking()
                .ToListAsync();

            var columns = new List<string>
            {
                "RowNumber",
                "EmployeeId",
                "CandidateName",
                "Email",
                "PAN",
                "Aadhaar",
                "Designation",
                "DOJ",
                "Status",
                "ErrorMessage"
             };

            var excel = ExportExcelBuildHelper.BuildExcel(
                failedRows,
                columns,
                (x, col) => ExcelColumnsHelper.GetOnboardingFailedColumnValue(x, col)
            );

            return (
                excel,
                $"OnboardingFailedRows_{batchId}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx"
            );
        }

        //----------------------------------insetion into doc table for confirmed candidates------------------------------
     

        public async Task<ConfirmedCandidateDto> ConfirmOnboardingBatchAsync(int batchId, int userId)
        {
            var batch = await _context.HrConfirmationBatches
                .Include(b => b.Rows)
                .FirstOrDefaultAsync(x => x.BatchId == batchId);

            if (batch == null)
                throw new ValidationException("Batch not found");

            if (batch.Status == "processed")
                throw new ValidationException("Batch already processed");

            var matchedRows = await _context.HrConfirmationBatchRows
                .Include(x => x.Candidate)
                    .ThenInclude(c => c.Request)
                        .ThenInclude(r => r.Collection)
                .Where(x =>
                    x.BatchId == batchId &&
                    x.Status == "matched")
                .ToListAsync();

            if (!matchedRows.Any())
                throw new ValidationException("No matched rows found");

            int inserted = 0;
            int skipped = 0;

            foreach (var row in matchedRows)
            {
                // Prevent duplicate insertion
                var alreadyExists = await _context.Documents.AnyAsync(d =>
                    d.CandidateId == row.CandidateId &&
                    d.EmployeeId == row.EmployeeId);

                if (alreadyExists)
                {
                    skipped++;
                    continue;
                }

                var document = new Document
                {
                    CandidateId = row.CandidateId,
                    CabinetId=2,
                    Region=row.Candidate.Request.Collection.Region,
                    // Basic onboarding details
                    Designation = row.Designation,
                    DOJ = row.DOJ,
                    EmployeeId = row.EmployeeId,
                    DOB=row.DOB,
                    ContactNumber=row.Phone,
                    Name = row.CandidateName,
                    
                    //FileName = string.Empty,
                    //FilePath = string.Empty,

                    Status = "active",
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = userId
                };

                _context.Documents.Add(document);
                row.IsConfirmed= true;
                row.Status = "converted";
                row.ConfirmedAt = DateTime.UtcNow;
                inserted++;
            }

            batch.Status = "processed";
            
            var reciepint= await _context.ConfigurationRequestRecipient
                .FirstOrDefaultAsync(x => x.Id == matchedRows.First().Candidate.Id);
            reciepint.IsHired = true;
            //batch.proccessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if(inserted>0 && skipped > 0)
            {
                return new ConfirmedCandidateDto
                {
                    BatchId = batchId,
                    Inserted = inserted,
                    Skipped = skipped,
                    Message = $"Successfully confirmed onboarding of {inserted} candidates and {skipped} candidates are skipped due to technical issues."
                };

            }
            else if(inserted > 0)
            {
                return new ConfirmedCandidateDto
                {
                    BatchId = batchId,
                    Inserted = inserted,
                    Skipped = skipped,
                    Message = $"Successfully confirmed onboarding of {inserted} candidates."
                };
            }
            else
            {
                return new ConfirmedCandidateDto
                {
                    BatchId = batchId,
                    Inserted = inserted,
                    Skipped = skipped,
                    Message = $"No candidates were confirmed due to technical issues. Please check the batch details and try again."
                };
            }


        }


        public async Task<OnboardingDocument> InternalDocumentUploadAsync(InternalUploaddto dto)
        {
            var recipient = await _context.ConfigurationRequestRecipient
                .FirstOrDefaultAsync(x => x.Id == dto.RecipientId);

            if (recipient == null)
                throw new NotFoundException("Recipient not found");

            var folderPath = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var safeCandidateName = recipient.Name
                .Trim()
                .Replace(" ", "_");

            var orgFolderName = $"{safeCandidateName}_{recipient.Token}";

            var finalPath = Path.Combine(folderPath, orgFolderName);

            if (!Directory.Exists(finalPath))
                Directory.CreateDirectory(finalPath);

            var finalFilePath = Path.Combine(finalPath, dto.FileName);

            File.Move(dto.TempFilePath, finalFilePath, true);

            var dbPath = Path.Combine(orgFolderName, dto.FileName);

            var entity = new OnboardingDocument
            {
                RecipientId = dto.RecipientId,
                DocumentTypeId = dto.DocumentTypeId,
                FilePath = dbPath,
                FileName = dto.FileName,
                UploadedAt = DateTime.UtcNow,
                Status = "active",
                Source = dto.Source
            };

            _context.OnboardingHRDocument.Add(entity);

            await _context.SaveChangesAsync();

            return entity;
        }


        public async Task<DocumentResponseDto> SplitOnboardingDocumentAsync(SplitAndExtractPdfDto dto)
        {
            if (dto.Source != DocumentSourceType.Onboarding)
                throw new BadRequestException("Invalid method");

            var originalDoc= await _repo.GetOnboardingFilesAsync(dto.Id);
            if (originalDoc == null)
                throw new NotFoundException("Document not found");


            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var relativePath = originalDoc.FilePath
                .TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            var fullPath = Path.Combine(uploadRootTemplate, relativePath);

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found");

            string tempExtractedPath = Path.GetTempFileName();
            string tempRemainingPath = Path.GetTempFileName();

            try
            {
                using var originalPdf =
                    PdfReader.Open(fullPath, PdfDocumentOpenMode.Import);

                if (dto.FromPage < 1 ||
                    dto.ToPage < 1 ||
                    dto.FromPage > dto.ToPage ||
                    dto.ToPage > originalPdf.PageCount)
                {
                    throw new ValidationException(
                        $"Invalid page range. PDF has {originalPdf.PageCount} pages.");
                }

                var extractedPdf = new PdfDocument();
                var remainingPdf = new PdfDocument();

                for (int i = 0; i < originalPdf.PageCount; i++)
                {
                    int pageNumber = i + 1;

                    if (pageNumber >= dto.FromPage &&
                        pageNumber <= dto.ToPage)
                    {
                        extractedPdf.AddPage(originalPdf.Pages[i]);
                    }
                    else
                    {
                        remainingPdf.AddPage(originalPdf.Pages[i]);
                    }
                }

                if (extractedPdf.PageCount == 0)
                    throw new ValidationException("No pages extracted");

                extractedPdf.Save(tempExtractedPath);
                remainingPdf.Save(tempRemainingPath);

                // replace original with remaining pages
                File.Move(tempRemainingPath, fullPath, true);

                var extension = Path.GetExtension(originalDoc.FileName);

                var originalName =
                    Path.GetFileNameWithoutExtension(originalDoc.FileName);

                var newFileName =
                    $"{dto.DocumentType}_{originalName}_{Guid.NewGuid():N}{extension}";
                var docType =await _docrepo.GetOrCreateDocLabelAsync(dto.DocumentType);
                var uploadDto = new InternalUploaddto
                {
                    RecipientId = originalDoc.RecipientId,
                    DocumentTypeId = docType.Id,
                    FileName = newFileName,
                    TempFilePath = tempExtractedPath,
                    Source = "hr_split"
                };

                var uploadedDoc =
                    await InternalDocumentUploadAsync(uploadDto);

                return new DocumentResponseDto
                {
                    DocumentId = uploadedDoc.Id,
                    FileName = uploadedDoc.FileName,
                    FilePath = uploadedDoc.FilePath,
                    Status = uploadedDoc.Status
                };
            }
            catch
            {
                if (File.Exists(tempExtractedPath))
                    File.Delete(tempExtractedPath);

                if (File.Exists(tempRemainingPath))
                    File.Delete(tempRemainingPath);

                throw;
            }
        }


        public async Task<(byte[], string)> ExportOnboardingReport(ExportOnboardingReportQuery query)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_export_onboarding_report(" +
                "@p_report_type," +
                "@p_region," +
                "@p_status," +
                "@p_config_id," +
                "@p_from_date," +
                "@p_to_date)",
                conn);

            cmd.Parameters.AddWithValue(
                "p_report_type",
                NpgsqlTypes.NpgsqlDbType.Text,
                (object?)query.ReportType ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "p_region",
                (object?)query.Region ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "p_status",
                (object?)query.Status ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "p_config_id",
                (object?)query.ConfigId ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "p_from_date",
                (object?)query.FromDate ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "p_to_date",
                (object?)query.ToDate ?? DBNull.Value);

            var rows = new List<OnboardingReportRowDto>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new OnboardingReportRowDto
                {
                    RecipientId = Convert.ToInt64(reader["recipient_id"]),
                    CandidateName = reader["candidate_name"]?.ToString(),
                    Email = reader["email"]?.ToString(),
                    Region = reader["region"]?.ToString(),
                    OverAllStatus = reader["overall_status"]?.ToString(),
                    CompletionPercent = Convert.ToDecimal(reader["completion_percent"]),
                    DocumentName = reader["document_name"]?.ToString(),
                    DocumentStatus = reader["document_status"]?.ToString(),
                    IsHired = Convert.ToBoolean(reader["is_hired"])
                });
            }

            var documentColumns = rows
                .Select(x => x.DocumentName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var exportData = rows
                .GroupBy(x => x.RecipientId)
                .Select(g =>
                {
                    var first = g.First();

                    var dto = new OnboardingReportExportDto
                    {
                        CandidateName = first.CandidateName,
                        Email = first.Email,
                        Region = first.Region,
                        OverAllStatus = first.OverAllStatus,
                        CompletionPercent = first.CompletionPercent,
                        IsHired = first.IsHired
                    };

                    foreach (var doc in documentColumns)
                    {
                        var docRow = g.FirstOrDefault(x => x.DocumentName == doc);

                        dto.Documents[doc] =
                            docRow == null
                                ? "N/A"
                                : docRow.DocumentStatus;
                    }

                    return dto;
                })
                .ToList();

            var columns = new List<string>
            {
                "CandidateName",
                "Email",
                "Region",
                "OverAllStatus",
                "IsHired",
                "CompletionPercent"
            };

            columns.AddRange(documentColumns);

            var excel = ExportExcelBuildHelper.BuildExcel(
                exportData,
                columns,
                (item, col) =>
                {
                    return col switch
                    {
                        "CandidateName" => item.CandidateName,
                        "Email" => item.Email,
                        "Region" => item.Region,
                        "OverAllStatus" => item.OverAllStatus,
                        "IsHired" => item.IsHired ? "Yes" : "No",
                        "CompletionPercent" => item.CompletionPercent,
                        _ => item.Documents.ContainsKey(col)
                                ? item.Documents[col]
                                : ""
                    };
                });

            return (
                excel,
                $"Onboarding_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
            );
        }


        public async Task<StatusCountResponseDto> GetCandidatesStatusCountAsync(StatusCountQueryParamDto dto)
        {
            var query = _context.ConfigurationRequestRecipient
                .AsNoTracking()
                .Where(r =>
                    dto.Region == null ||
                        r.Request.Collection.Region == dto.Region);
            if (dto.FromDate.HasValue)
            {
                query = query.Where(r => r.Request.CreatedAt >= dto.FromDate.Value);
            }
            if (dto.ToDate.HasValue)
            {
                query = query.Where(r => r.Request.CreatedAt <= dto.ToDate.Value);
            }
            var stats = await query
                .GroupBy(x => 1)
                .Select(g => new StatusCountResponseDto
                {
                    Total = g.Count(),

                    Pending = g.Count(x =>
                        x.Status== "pending"),

                    Completed = g.Count(x =>
                        x.Status== "completed"),

                    InProgress = g.Count(x =>
                        x.Status == "inProgress"),
                    
                    Expired= g.Count(x =>
                        x.Status == "expired")
                })
                .FirstOrDefaultAsync();
            //return new StatusCountResponseDto
            //{
            //    Total = stats.Total,
            //    Pending = stats.Pending,
            //    Completed = stats.Completed,
            //    InProgress = stats.InProgress,
            //    Expired= stats.Expired
            //};
            return stats ?? new StatusCountResponseDto();
        }

        //-------------------------helpers---------------------------------------
        private static void ValidateHeaders(List<string> headers)
        {
            var requiredHeaders = new[]
            {
                "employeeid",
                 "designation",
                 "doj",
                "email",
                //"filename",
                "aadhaar",
                "pan",
                //"dob"
             };

            var missingHeaders = requiredHeaders
                .Except(headers,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingHeaders.Any())
            {
                throw new ValidationException(
                    $"Missing required columns: {string.Join(", ", missingHeaders)}");
            }

        }

        private static void ValidateRow(HrParsedRowDto row)
        {
            if (string.IsNullOrWhiteSpace(row.EmployeeId))
                throw new ValidationException("EmployeeId is required");

            if (string.IsNullOrWhiteSpace(row.Designation))
                throw new ValidationException("Designation is required");

            if (row.DOJ == null)
                throw new ValidationException("DOJ is required");

            if (string.IsNullOrWhiteSpace(row.Email))
                throw new ValidationException("Email is required");

            if (string.IsNullOrWhiteSpace(row.Aadhaar))
                throw new ValidationException("Aadhaar is required");

            if (string.IsNullOrWhiteSpace(row.PAN))
                throw new ValidationException("PAN is required");
        }

    }
}
