using EVWebApi.Data;
using EVWebApi.DTOs.HR;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Models.HR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


namespace EVWebApi.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly AppDbContext _context;
        private readonly IDocumentRepository _docrepo;
        private readonly IConfigurationRepository _repo;
        private readonly IEmailSender _emailSender;
        private readonly string _externalUploadUrl;
        private readonly string _uploadRoot;
        private readonly string _storageRoot;
        private readonly string _clientName;
        public ConfigurationService(AppDbContext context, IDocumentRepository docrepo, IConfigurationRepository repo, IEmailSender emailSender, IConfiguration config)
        {
            _context = context;
            _docrepo = docrepo;
            _repo = repo;
            _emailSender = emailSender;
            _externalUploadUrl = config["DocumentSettings:ExternalUploadURL"];
            _uploadRoot = config["DocumentSettings:OnboardingFilePath"];
            _storageRoot = config["DocumentSettings:StorageRoot"];
            _clientName = config["DocumentSettings:ClientName"];
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
                CollectionId = dto.CollectionId,
                ExpiryDate = dto.ExpiryDate,
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
                    Status = "Pending"
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
                <p>Hello,</p>
                <p>Please upload your onboarding documents using the link below:</p>
                <p><a href='{link}'>Upload Documents</a></p>
                <p>This link will expire on {dto.ExpiryDate}.</p>
            ";
                var subject = "Onboarding Document Upload";
                await semaphore.WaitAsync();
                try
                {
                    var sent = await _emailSender.SendAsync(r.Email, ReplyTo: user.Email, UserName: user.Username, subject, body);

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
                recipient.Status = "Expired";
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


            if (recipient.Request.ExpiryDate < DateTime.UtcNow)
            {
                recipient.Status = "Expired";
                await _context.SaveChangesAsync();
                throw new Exception("Link expired");
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

                var fileName = $"{Guid.NewGuid()}_{doc.File.FileName}";
                var folderPath = _uploadRoot
                    .Replace("{StorageRoot}", _storageRoot)
                    .Replace("{ClientName}", _clientName);
                var folderName = $"{dto.Name}_{dto.Token}";
                var finalPath= Path.Combine(folderPath, folderName);
                if (!Directory.Exists(finalPath))
                    Directory.CreateDirectory(finalPath);
                var safeName = Regex.Replace(folderName, @"[^a-zA-Z0-9_-]", "");
                var filePath = Path.Combine(finalPath, fileName);
                var dbPath= Path.Combine(safeName, fileName);
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
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.SaveChangesAsync();

            var totalDocs = recipient.Request.Collection.CollectionDocumentTypes.Count;

            var uploadedCount = await _repo.GetUploadCount(recipient.Id);

            if (uploadedCount == 0)
                recipient.Status = "Pending";
            else if (uploadedCount < totalDocs)
                recipient.Status = "InProgress";
            else
            {
                recipient.Status = "Completed";
                recipient.CompletedAt = DateTime.UtcNow;
            }
            recipient.AccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();


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
                    Pending = r.Recipients.Count(x => x.Status == "Pending"),
                    InProgress = r.Recipients.Count(x => x.Status == "InProgress"),
                    Completed = r.Recipients.Count(x => x.Status == "Completed")
                })
                .ToListAsync();

           
        }

        public async Task<ConfigRequestDetailsDto> GetConfigRequestByIdAsync(int requestId, ConfigQueryDetailDto dto)
        {
            var request = await _repo.GetConfigRequestByIdAsync(requestId,dto?.Status);

            if (request == null)
                throw new Exception("Request not found");
            var total = request.Collection?.CollectionDocumentTypes?.Count ?? 0;

            var recipientIds = request.Recipients.Select(r => r.Id).ToList();

            var uploadCounts = await _context.OnboardingHRDocument
                .Where(x => recipientIds.Contains(x.RecipientId))
                .GroupBy(x => x.RecipientId)
                .Select(g => new
                {
                    RecipientId = g.Key,
                    Count = g.Select(x => x.DocumentTypeId).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.RecipientId, x => x.Count);


            var recipients =
            request.Recipients.Select( r =>
            {
                var submitted = uploadCounts.ContainsKey(r.Id) ? uploadCounts[r.Id] : 0;

                return new RecipientDto
                {
                    RecipientId = r.Id,
                    Email = r.Email,
                    Name = r.Name,
                    PAN = r.PAN,
                    Adhaar = r.Adhaar,
                    Submitted = submitted ,
                    Pending = total - submitted,
                    Status = r.Status,
                    CompletedAt = r.CompletedAt
                };
            }).ToList() ?? new List<RecipientDto>();

            return new ConfigRequestDetailsDto
            {
                ConfigId = request.Id,
                CollectionName = request.Collection?.Name,
                TotalDocs = total,
                CreatedAt = request.CreatedAt,
                Description = request.Description,
                Recipients = recipients
            };
        }
    }
}
