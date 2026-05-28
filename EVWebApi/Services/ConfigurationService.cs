using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
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
using EVWebApi.Services;
using Humanizer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Org.BouncyCastle.Cms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Serilog;
using Syncfusion.EJ2.Grids;
using Syncfusion.EJ2.Spreadsheet;
using Syncfusion.XlsIO;
using System.Data.Common;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static QRCoder.PayloadGenerator;

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
        private readonly string _tempRoot;
        private readonly string _baseUrl;
        private readonly string _backendBaseUrl;
        private readonly string _storageRoot;
        private readonly string _clientName;
        private readonly string _profilePic;
        private readonly NpgsqlDataSource _dataSource;
        private readonly IUnitOfWork _uow;
        public ConfigurationService(AppDbContext context, IDocumentRepository docrepo, IConfigurationRepository repo, IEmailSender emailSender, IConfiguration config,
            IMetadataReaderFactoryService metadataReaderFactory, INotificationService notificationService, NpgsqlDataSource dataSource, IUnitOfWork uow)
        {
            _context = context;
            _docrepo = docrepo;
            _repo = repo;
            _emailSender = emailSender;
            _notificationService = notificationService;
            _metadataReaderFactory = metadataReaderFactory;
            _baseUrl = config["Frontend:BaseUrl"];
            _backendBaseUrl = config["BackEnd:BaseUrl"];
            _externalUploadUrl = config["DocumentSettings:ExternalUploadURL"];
            _uploadRoot = config["DocumentSettings:OnboardingFilePath"];
            _tempRoot = config["DocumentSettings:TempPath"];
            _storageRoot = config["DocumentSettings:StorageRoot"];
            _clientName = config["DocumentSettings:ClientName"];
            _profilePic = config["DocumentSettings:ProfileImagesPath"];
            _dataSource = dataSource;
            _uow = uow;
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
                var docTypeDto = new DocTypeCreateDto
                {
                    Label = type.Trim(),
                    Type = dto.Type
                };
                var docType = await _docrepo.GetOrCreateDocLabelAsync(docTypeDto);
                docTypeEntities.Add(docType);
            }

            //mandatory docs
            if (dto.Type == "pre")
            {
                var mandatoryDoc = await _context.DocumentTypes
                .FirstOrDefaultAsync(x => x.Key == "passport_size_photograph");

                if (mandatoryDoc != null &&
                    !docTypeEntities.Any(x => x.Id == mandatoryDoc.Id))
                {
                    docTypeEntities.Add(mandatoryDoc);
                }
            }

            var collection = new DocumentCollection
            {
                Name = dto.Name,
                Designation = dto.Designation,
                Region = dto.Region,
                Status = "active",
                IsExternal = dto.IsExternal,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.Value,
                Type = dto.Type,
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
                Region = collection.Region,
                Designation = collection.Designation,
                CreatedAt = collection.CreatedAt,
                CreatedBy = collection.CreatedBy,
                Type = collection.Type,
                Status= collection.Status,
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

            if (dto.Type != collection.Type)
                throw new BadRequestException("Collection Type can't be changed as it have associated documents");


            var docTypeEntities = new List<DocumentTypes>();
            foreach (var type in dto.DocumentTypes)
            {
                if (string.IsNullOrWhiteSpace(type))
                    continue;
                var docTypeDto = new DocTypeCreateDto
                {
                    Label = type.Trim(),
                    Type = dto.Type
                };
                var docType = await _docrepo.GetOrCreateDocLabelAsync(docTypeDto);
                docTypeEntities.Add(docType);
            }
            collection.Name = dto.Name;
            collection.Designation = dto.Designation;
            collection.Region = dto.Region;
            //collection.Type = dto.Type;
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
                IsExternal = collection.IsExternal,
                Status = collection.Status,
                Designation = collection.Designation,
                Region = collection.Region,
                CreatedAt = collection.CreatedAt,
                CreatedBy = collection.CreatedBy,
                DocumentTypes = docTypeEntities.Select(x => x.Label).ToList()
            };
        }


        public async Task<PagedResponse<CollectionListResponseDto>> GetCollectionListAsync(CollectionQueryDto dto)
        {
            var query = _repo.Query();
            if (!string.IsNullOrWhiteSpace(dto.Type))
            {
                query = query.Where(c => c.Type.ToLower() == dto.Type.ToLower());
            }
            //else query = query.Where(c => c.Type.ToLower() == "pre");



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
            if (dto.IsExternal == true)
                query = query.Where(c => c.IsExternal == true);
            else if (dto.IsExternal == false)
                query = query.Where(c => c.IsExternal == false);
            else
                query = query.Where(c => c.IsExternal == false || c.IsExternal == true);


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
                Region = c.Region,
                Designation = c.Designation,
                Status = c.Status,
                Type = c.Type,
                IsExternal = c.IsExternal,
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
                .Where(d => d.Status.ToLower() == "active")
                .AsQueryable();


            if (dto.IsExternal.HasValue)
            {
                query = query.Where(d => d.IsExternal == dto.IsExternal.Value);
            }

            if (!string.IsNullOrWhiteSpace(dto.Region))
            {
                query = query.Where(d => d.Region == dto.Region);
            }

            return await query
                .Select(d => new CollectionDropDownDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Region = d.Region,
                    Type = d.Type
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
                Status = collection.Status,
                Type = collection.Type,
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
        //------------------------------------send url---------------------------------------------
        //public async Task<ConfigurationResponseDto> SendConfigurationAsync(ConfigurationRequestDto dto, int userId)
        //{

        //    await using var transaction = await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        var collection = await _repo.GetCollectionByIdAsync(dto.CollectionId);

        //        if (collection == null)
        //            throw new NotFoundException("Collection not found");

        //        var user = await _context.Users
        //            .Where(u => u.UserId == userId)
        //            .Select(u => new
        //            {
        //                u.Email,
        //                u.Username
        //            })
        //            .FirstOrDefaultAsync();

        //        // Create Request
        //        var request = new ConfigRequest
        //        {
        //            ConfigName = dto.Name,
        //            CollectionId = dto.CollectionId,
        //            ExpiryDate = dto.ExpiryDate!.Value,
        //            Description = dto.Description,
        //            CreatedBy = userId,
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        // Normalize emails
        //        var normalizedEmails = dto.Emails
        //            .Where(e => !string.IsNullOrWhiteSpace(e))
        //            .Select(e => e.Trim().ToLower())
        //            .Distinct()
        //            .ToList();

        //        var candidates = new List<Candidate>();
        //        var existingCandidates = await _context.Candidates
        //            .Where(c => normalizedEmails.Contains(c.Email.ToLower()))
        //            .ToListAsync();

        //        // PRE ONBOARDING
        //        if (collection.Type.ToLower() == "pre")
        //        {
        //            var existingEmails = existingCandidates
        //                .Select(c => c.Email.ToLower())
        //                .ToList();
        //            if (existingEmails.Any())
        //            {
        //                throw new BadRequestException(
        //                    $"Candidates already exist: {string.Join(", ", existingEmails)}");
        //            }

        //            candidates = normalizedEmails
        //                .Select(email => new Candidate
        //                {
        //                    Email = email,
        //                    Region = collection.Region,
        //                    CreatedAt = DateTime.UtcNow
        //                })
        //                .ToList();

        //            _context.Candidates.AddRange(candidates);

        //            await _context.SaveChangesAsync();
        //        }

        //        // POST ONBOARDING
        //        else
        //        {
        //            candidates = existingCandidates;


        //            var foundEmails = candidates
        //                .Select(c => c.Email.ToLower())
        //                .ToList();

        //            var missingEmails = normalizedEmails
        //                .Except(foundEmails)
        //                .ToList();

        //            if (missingEmails.Any())
        //            {
        //                throw new BadRequestException(
        //                    $"Candidates not found for post onboarding: {string.Join(", ", missingEmails)}");//For post onboarding, all provided email IDs must already exist in Candidates table.
        //            }

        //            var notHiredCandidates = candidates
        //                .Where(c => !c.IsHired)
        //                .Select(c => c.Email)
        //                .ToList();

        //            if (notHiredCandidates.Any())
        //            {
        //                throw new BadRequestException(
        //                    $"Post onboarding documents can only be assigned to hired candidates. Not hired: {string.Join(", ", notHiredCandidates)}");
        //            }
        //        }

        //        // Create recipients


        //        var recipients = candidates
        //            .Select(candidate => new ConfigRequestRecipient
        //            {
        //                Token = Guid.NewGuid().ToString(),
        //                CandidateId = candidate.Id,
        //                Status = "pending"

        //            })
        //            .ToList();

        //        request.Recipients = recipients;
        //        _context.ConfigurationRequests.Add(request);

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();


        //        //Generate email tasks (parallel)

        //        int success = 0;
        //        int failed = 0;
        //        var failedDetails = new List<string>();
        //        var lockObj = new object();

        //        var semaphore = new SemaphoreSlim(5); // max 5 parallel
        //        var emailTasks = recipients.Select(async r =>
        //        {
        //            var link = $"{_externalUploadUrl}/{r.Token}";

        //            var body = $@"
        //            <p>Greetings,</p>
        //            <p>Please upload the required documents using the link below:</p>
        //            <p><a href='{link}'><b>Upload Your Documents</b></a></p>
        //            <p style='font-size:13px;color:#FF0000;'><i>Note : This link will expire on {dto.ExpiryDate}.</i></p>
        //            <br><p>If you need any assistance, feel free to contact us.</p></br>

        //            <p>Regards,</p>
        //            <p>Apollo EIPP Vault Team</p>
        //        ";
        //            var subject = "Action Required: Upload Your Documents";
        //            await semaphore.WaitAsync();
        //            try
        //            {
        //                var sent = await _emailSender.SendAsync(r.Candidate.Email, ReplyTo: user.Email, UserName: "Apollo OnBoarding", subject, body);

        //                lock (lockObj)
        //                {
        //                    if (sent)
        //                        success++;
        //                    else
        //                        failed++;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                lock (lockObj)
        //                {
        //                    failed++;
        //                    failedDetails.Add($"Email failed for {r.Candidate.Email}: {ex.Message}");
        //                }
        //            }
        //            finally
        //            {
        //                semaphore.Release();
        //            }
        //        });

        //        // Send all emails in parallel
        //        await Task.WhenAll(emailTasks);


        //        return new ConfigurationResponseDto
        //        {
        //            RequestId = request.Id,
        //            TotalEmails = recipients.Count,
        //            Success = success,
        //            Failed = failed,
        //            FailedEmailDetails = failedDetails

        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        throw new ServerException(ex.InnerException.Message);
        //    }
        //}
        public async Task<ConfigurationResponseDto> SendConfigurationAsync(ConfigurationRequestDto dto, int userId)
        {

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var collection = await _repo.GetCollectionByIdAsync(dto.CollectionId);

                if (collection == null)
                    throw new NotFoundException("Collection not found");

                var user = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new
                    {
                        u.Email,
                        u.Username
                    })
                    .FirstOrDefaultAsync();

                // Create Request
                var request = new ConfigRequest
                {
                    ConfigName = dto.Name,
                    CollectionId = dto.CollectionId,
                    ExpiryDate = dto.ExpiryDate!.Value,
                    Description = dto.Description,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                // Normalize emails
                var normalizedEmails = dto.Emails
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim().ToLower())
                    .Distinct()
                    .ToList();

                var candidates = new List<Candidate>();
                var existingCandidates = await _context.Candidates
                    .Where(c => normalizedEmails.Contains(c.Email.Trim().ToLower()))
                    .ToListAsync();

                // PRE ONBOARDING
                if (collection.Type.ToLower() == "pre")
                {
                    var existingEmails = existingCandidates
                        .Select(c => c.Email.ToLower())
                        .ToList();
                    var newEmails = normalizedEmails
                         .Where(e => !existingEmails.Contains(e))
                        .ToList();


                    var newCandidates = newEmails
                        .Select(email => new Candidate
                        {
                            Email = email,
                            Region = collection.Region,
                            CreatedAt = DateTime.UtcNow
                        })
                        .ToList();

                    if (newCandidates.Any())
                    {
                        _context.Candidates.AddRange(newCandidates);
                        await _context.SaveChangesAsync();
                    }
                    candidates = existingCandidates
                        .Concat(newCandidates)
                        .ToList();
                    //if (existingEmails.Any())
                    //{
                    //    throw new BadRequestException(
                    //        $"Candidates already exist: {string.Join(", ", existingEmails)}");
                    //}

                    //candidates = normalizedEmails
                    //    .Select(email => new Candidate
                    //    {
                    //        Email = email,
                    //        Region = collection.Region,
                    //        CreatedAt = DateTime.UtcNow
                    //    })
                    //    .ToList();

                }

                // POST ONBOARDING
                else
                {
                    candidates = existingCandidates;


                    var foundEmails = candidates
                        .Select(c => c.Email.ToLower())
                        .ToList();

                    var missingEmails = normalizedEmails
                        .Except(foundEmails)
                        .ToList();

                    if (missingEmails.Any())
                    {
                        throw new BadRequestException(
                            $"Candidates not found for post onboarding: {string.Join(", ", missingEmails)}");//For post onboarding, all provided email IDs must already exist in Candidates table.
                    }

                    var notHiredCandidates = candidates
                        .Where(c => !c.IsHired)
                        .Select(c => c.Email)
                        .ToList();

                    if (notHiredCandidates.Any())
                    {
                        throw new BadRequestException(
                            $"Post onboarding documents can only be assigned to hired candidates. Not hired: {string.Join(", ", notHiredCandidates)}");
                    }
                }

                // Create recipients


                var recipients = candidates
                    .Select(candidate => new ConfigRequestRecipient
                    {
                        Token = Guid.NewGuid().ToString(),
                        CandidateId = candidate.Id,
                        Status = "pending"

                    })
                    .ToList();

                request.Recipients = recipients;
                _context.ConfigurationRequests.Add(request);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


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
                        var sent = await _emailSender.SendAsync(r.Candidate.Email, ReplyTo: user.Email, UserName: "Apollo OnBoarding", subject, body);

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
                            failedDetails.Add($"Email failed for {r.Candidate.Email}: {ex.Message}");
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
                    Success = success,
                    Failed = failed,
                    FailedEmailDetails = failedDetails

                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new ServerException(ex.InnerException?.Message ?? ex.Message);
            }
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
                RequestId = recipient.Id,
                CandidateId= recipient.CandidateId,
                Email = recipient.Candidate.Email,
                Name = recipient.Candidate.Name,
                DOB = recipient.Candidate.DateOfBirth?.ToString("yyyy-MM-dd"),
                Phone = recipient.Candidate.Phone,
                PAN= recipient.Candidate.PAN,
                Adhaar= recipient.Candidate.Adhaar,
                Designation = recipient.Request.Collection.Designation,
                IsExternal = recipient.Request.Collection.IsExternal,
                CollectionName = recipient.Request.Collection.Name,
                CollectionType = recipient.Request.Collection.Type,
                Status = statusDto,
                Documents = documents
            };
        }
        //upload docs ------------------------------------------------------------------------------------------------------

        public async Task<UploadResultDto> MainUploadDocumentsAsync(OnboardingDocsDto dto)
        {
            var recipient = await _repo.GetConfigRequestByToken(dto.Token);


            await ValidateRecipientAsync(recipient, dto);

            var requestType = recipient.Request.Collection.Type?.ToLower();

            if (requestType == "pre")
            {
                await HandlePreOnboardingUploadAsync(recipient, dto);
            }
            else
            {
                await HandlePostOnboardingUploadAsync(recipient, dto);
            }

            await UpdateRecipientStatusAsync(recipient);

            await SendNotificationsAsync(recipient);

            return BuildUploadResult(recipient);
        }
        //public async Task<UploadResultDto> UploadDocumentsAsync(OnboardingDocsDto dto)
        //{
        //    var recipient = await _repo.GetConfigRequestByToken(dto.Token);

        //    if (recipient == null)
        //        throw new Exception("Invalid token");

        //    if (!string.IsNullOrWhiteSpace(dto.Email))
        //        if (dto.Email != recipient.Email) throw new BadRequestException("Provided email doesn't match with the one given while applying for the job.");
        //    var assignedDocsId = recipient.Request.Collection.CollectionDocumentTypes.Select(i => i.DocumentTypeId);
        //    if (recipient.Request.ExpiryDate < DateTime.UtcNow)
        //    {
        //        recipient.Status = "expired";
        //        await _context.SaveChangesAsync();
        //        throw new BadRequestException("Link expired");
        //    }

        //    recipient.Name = dto.Name;
        //    recipient.Phone = dto.Phone;
        //    recipient.DateOfBirth = dto.Dob;
        //    recipient.Adhaar = dto.AdhaarNo;
        //    recipient.PAN = dto.PAN;
        //    foreach (var doc in dto.Files)
        //    {
        //        if (doc.File == null || doc.File.Length == 0)
        //            continue;
        //        if (assignedDocsId.Contains(doc.DocumentTypeId))
        //        {

        //            //var fileName = $"{Guid.NewGuid()}_{doc.File.FileName}";
        //            var folderPath = _uploadRoot
        //                .Replace("{StorageRoot}", _storageRoot)
        //                .Replace("{ClientName}", _clientName);

        //            //var folderName = $"{dto.Name}_{dto.Token}";
        //            var safecandidateName = dto.Name.Trim().Replace(" ", "_");
        //            var orgfolderName = $"{safecandidateName}_{dto.Token}";

        //            var finalfolderPath = Path.Combine(folderPath, orgfolderName);

        //            if (!Directory.Exists(finalfolderPath))
        //                Directory.CreateDirectory(finalfolderPath);

        //            var originalExtension = Path.GetExtension(doc.File.FileName);

        //            // safer doc type label
        //            var docTypeLabel = recipient.Request.Collection.CollectionDocumentTypes
        //                .First(x => x.DocumentTypeId == doc.DocumentTypeId)
        //                .DocumentType.Label
        //                .Trim()
        //                .Replace(" ", "_");

        //            // display filename
        //            var displayFileName = $"{safecandidateName}_{docTypeLabel}{originalExtension}";


        //            var filePath = Path.Combine(finalfolderPath, displayFileName);
        //            var dbPath = Path.Combine(orgfolderName, displayFileName);
        //            using (var stream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await doc.File.CopyToAsync(stream);
        //            }

        //            //Replace if already exists
        //            var existing = recipient.UploadedDocuments
        //                .FirstOrDefault(x => x.DocumentTypeId == doc.DocumentTypeId);

        //            if (existing != null)
        //            {
        //                existing.FilePath = dbPath;
        //                existing.UploadedAt = DateTime.UtcNow;
        //            }
        //            else
        //            {
        //                _context.OnboardingHRDocument.Add(new OnboardingDocument
        //                {
        //                    RecipientId = recipient.Id,
        //                    DocumentTypeId = doc.DocumentTypeId,
        //                    FilePath = dbPath,
        //                    FileName = displayFileName,
        //                    UploadedAt = DateTime.UtcNow,
        //                    Status = "active",
        //                    Source = "candidate_upload"
        //                });
        //            }
        //        }
        //        else
        //        {
        //            throw new NotFoundException("Uploaded files includes documents which is not assigned to the recipient");
        //        }
        //    }
        //    await _context.SaveChangesAsync();

        //    var totalDocs = recipient.Request.Collection.CollectionDocumentTypes.Count;

        //    var uploadedCount = await _repo.GetUploadCount(recipient.Id);

        //    if (uploadedCount == 0)
        //        recipient.Status = "pending";
        //    else if (uploadedCount < totalDocs)
        //        recipient.Status = "inProgress";
        //    else
        //    {
        //        recipient.Status = "completed";
        //        recipient.CompletedAt = DateTime.UtcNow;
        //    }
        //    recipient.AccessedAt = DateTime.UtcNow;
        //    await _context.SaveChangesAsync();

        //    // Notify HR after successful upload

        //    if (recipient.Status == "completed")
        //    {
        //        await _notificationService.CreateAsync(
        //            recipient.Request.CreatedBy,
        //            $"Candidate {recipient.Name} completed all document uploads"
        //        );


        //        var hrUser = await _context.Users
        //            .FirstOrDefaultAsync(u => u.UserId == recipient.Request.CreatedBy);

        //        if (hrUser != null && !string.IsNullOrEmpty(hrUser.Email))
        //        {
        //            var subject = "Candidate Document Submission Completed";
        //            var link = $"{_baseUrl}";
        //            var body = $@"
        //                Hello {hrUser.FirstName},

        //                The candidate <b>{recipient.Name}</b> has successfully uploaded all required documents.

        //                <p><b>Candidate Email:</b> {recipient.Email}</p>
        //                <b>Completion Time:</b> {DateTime.UtcNow}

                        
        //                <p><a href='{link}'>Please log in to review the documents.</a></p>


        //                <br><p><i>This is a system-generated email. Please do not reply.</i></p></br>
        //                Regards,<br/>
        //                Apollo EIPP Vault Team
        //             ";

        //            await _emailSender.SendAsync(hrUser.Email, null, null, subject, body);
        //        }
        //    }
        //    else
        //    {
        //        await _notificationService.CreateAsync(
        //            recipient.Request.CreatedBy,
        //            $"Candidate {recipient.Name} uploaded documents, Status - In Progress"
        //        );
        //    }

        //    var documents = recipient.Request.Collection.CollectionDocumentTypes
        //        .Select(cd => new DocumentTypeDto
        //        {
        //            DocumentTypeId = cd.DocumentTypeId,
        //            DocType = cd.DocumentType.Label,
        //            Uploaded = recipient.UploadedDocuments
        //                .Any(u => u.DocumentTypeId == cd.DocumentTypeId)
        //        })
        //        .ToList();

        //    return new UploadResultDto
        //    {
        //        Status = recipient.Status,
        //        Documents = documents
        //    };


        //}

        public async Task<List<CompletedRecipientDto>> GetRecipientDetailsAsync(int candidateId)
        {
            var profilePicDocTypeId = _context.DocumentTypes
                .FirstOrDefault(x => x.Key == "passport_size_photograph")
                ?.Id;

            
            return await _repo.GetCandidateDocsById(candidateId)
                .Select(c => new CompletedRecipientDto
            {
                RecipientId = c.Id,
                Email = c.Email,
                Name = c.Name,
                Phone = c.Phone,
                Adhaar = c.Adhaar,
                PAN = c.PAN,
                Dob = c.DateOfBirth,
                Status = c.Status,
                IsHired = c.IsHired,
                IsLaptopRequestSent = c.IsLaptopRequestSent,
                Documents = c.OnboardingDocs.Select(d => new DocumentTypeDetailDto
                {
                   
                    DocumentTypeId = d.DocumentTypeId,
                    DocType = d.DocumentType.Label,
                    FilePath = d.FilePath,
                    FileUrl = d.DocumentType.Key == "passport_size_photograph"
                        ? $"{_backendBaseUrl}/images/{d.FilePath}"
                        : $"{_backendBaseUrl}/api/documents/preview/{d.Id}",
                    Category = d.DocumentType.Category,
                    FileId =d.Id
                }).ToList()
                
            }).ToListAsync();

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
                .Where(x => allRecipientIds.Contains(x.RecipientId.Value))
                .Select(x => new
                {
                    x.CandidateId,
                    x.RecipientId,
                    x.DocumentTypeId,
                    x.Id,
                    x.FilePath,
                    x.Recipient,
                    DocType = x.DocumentType.Label,
                    Category = x.DocumentType.Category
                })
                .ToListAsync();

            foreach (var request in requests)
            {
                var docTypes = request.Collection?.CollectionDocumentTypes
                    .Select(dt => new DocumentTypeListDto
                    {
                        DocumentTypeId = dt.DocumentTypeId,
                        DocType = dt.DocumentType.Label,
                        Category = dt.DocumentType.Category
                    })
                    .ToList() ?? new List<DocumentTypeListDto>();

                //var recipientIds = request.Recipients.Select(r => r.CandidateId).ToList();

                //var uploadedDocs = allUploadedDocs
                //    .Where(x => recipientIds.Contains(x.CandidateId) && x.RecipientId == request.Id)
                //    .ToList();

                var recipients = request.Recipients.Select(r =>
                {
                    var userDocs = allUploadedDocs
                    .Where(x =>
                        //x.CandidateId == r.CandidateId &&
                        x.RecipientId == r.Id);

                    var submittedDocuments = userDocs
                        .Select(x => new DocumentTypeDetailDto
                        {
                            DocumentTypeId = x.DocumentTypeId,
                            DocType = x.DocType,
                            FileId = x.Id,
                            FilePath = x.FilePath,
                            Category = x.Category
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
                        RecipientId = r.CandidateId,
                        Email = r.Candidate.Email,
                        Name = r.Candidate.Name,
                        Phone=r.Candidate.Phone,
                        PAN = r.Candidate.PAN,
                        Adhaar = r.Candidate.Adhaar,
                        Dob = r.Candidate.DateOfBirth,
                        Status = r.Status,
                        CompletedAt = r.CompletedAt,
                        IsHired = r.Candidate.IsHired,
                        IsLaptopRequestSent = r.Candidate.IsLaptopRequestSent,
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
                    ConfigName = request.ConfigName,
                    CollectionName = request.Collection?.Name,
                    CollectionType = request.Collection?.Type,
                    Region = request.Collection?.Region,
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
            //if (doc == null)
            //    throw new NotFoundException("Document not found");

            var relativePath = doc.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());
            string fileName = Path.GetFileName(relativePath);
            string? uploadRootTemplate;
            if (doc.DocumentType?.Key == "passport_size_photograph")
            {
                uploadRootTemplate = _profilePic
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);
            }
            else
            {
                 uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);
            }

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

        //open excel------------------------------
        public async Task<OpenExcelDto> GetOnboardingExcelSheetNamesAsync(DocumentRequestDto dto)
        {
          
            var doc = await _repo.GetOnboardingFilesAsync(dto.Id);
            if (doc == null)
                throw new NotFoundException("Document not found");


            var relativePath = doc.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());

            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var fullPath = Path.Combine(uploadRootTemplate, relativePath);

            if (!fullPath.StartsWith(_storageRoot))
                throw new SecurityException("Invalid file path");

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");


            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".xls", ".xlsx", ".xlsb", ".xlsm", ".xltx", ".xltm"
            };

            var extension = Path.GetExtension(doc.FileName);

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid Excel file extension/File is not an excel.");
            }


            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;


            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var workbook = application.Workbooks.Open(stream);

            //var sheetNames = workbook.Worksheets.Select(s => s.Name).ToList();
            var sheets = workbook.Worksheets
                .Select((sheet, index) => new ListDto
                {
                    Id = index,
                    Name = sheet.Name
                })
                .ToList();

            return new OpenExcelDto
            {
                Source = dto.Source,
                Sheets = sheets
            };
        }
        public async Task<string> OpenOnboardingExcelSheetAsync(DocumentExcelOpenDTO dto)
        {
            var document = await _repo.GetOnboardingFilesAsync(dto.DocumentId);
            if (document == null)
                throw new NotFoundException("Document not found");


            var relativePath = document.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());

            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var fullPath = Path.Combine(uploadRootTemplate, relativePath);

            if (!fullPath.StartsWith(_storageRoot))
                throw new SecurityException("Invalid file path");

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".xls", ".xlsx", ".xlsb", ".xlsm", ".xltx", ".xltm"
            };

            var extension = Path.GetExtension(document.FileName);

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid Excel file extension/File is not an excel.");
            }


            int requestedRowCount = Math.Clamp(dto.RowCount, 1, 500);
            int startRow = Math.Max(dto.StartRow, 1);

            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var workbook = application.Workbooks.Open(stream);

            if (dto.SheetIndex < 0 || dto.SheetIndex >= workbook.Worksheets.Count)
                throw new KeyNotFoundException($"Invalid sheet index, total sheet count is {workbook.Worksheets.Count}");


            var sheet = workbook.Worksheets[dto.SheetIndex];

            int lastRow = sheet.UsedRange.LastRow;
            int lastCol = sheet.UsedRange.LastColumn;

            if (startRow > lastRow)
            {
                return JsonSerializer.Serialize(new
                {
                    sheetName = sheet.Name,
                    sheetIndex = dto.SheetIndex,
                    startRow,
                    rowCount = 0,
                    totalRows = lastRow,
                    totalColumns = lastCol,
                    cells = new List<object>()
                });
            }

            int endRow = Math.Min(startRow + requestedRowCount - 1, lastRow);
            var cells = new List<object>((endRow - startRow + 1) * lastCol);

            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = 1; c <= lastCol; c++)
                {
                    var cell = sheet.Range[r, c];

                    if (string.IsNullOrWhiteSpace(cell.DisplayText))
                        continue;

                    var style = cell.CellStyle;

                    // Font color
                    string fontColor =
                        style.Font.RGBColor != null
                            ? $"#{style.Font.RGBColor}"
                            : null;

                    // Background color
                    string backgroundColor =
                        !style.Color.IsEmpty
                            ? $"#{style.Color.R:X2}{style.Color.G:X2}{style.Color.B:X2}"
                            : null;

                    cells.Add(new
                    {
                        row = r,
                        col = c,
                        value = cell.DisplayText,
                        style = new
                        {
                            bold = style.Font.Bold,
                            italic = style.Font.Italic,
                            fontColor,
                            backgroundColor,
                            hAlign = style.HorizontalAlignment.ToString()
                        }
                    });
                }
            }

            var response = new
            {
                sheetName = sheet.Name,
                sheetIndex = dto.SheetIndex,
                startRow,
                rowCount = endRow - startRow + 1,
                totalRows = lastRow,
                totalColumns = lastCol,
                cells
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }


        public async Task<BatchResponseDTO> ApplyOboardingExcelPatchAsync(ExcelPatchRequestDto dto, int? userId)
        {
            var response = new BatchResponseDTO();
            var document = await _repo.GetOnboardingFilesAsync(dto.DocumentId);
            if (document == null || document.Status == "archived")
                throw new Exception("Document not found");

            //var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            //if (cabinet == null || document.CabinetId != dto.CabinetId)
            //    throw new Exception("Invalid CabinetId");


            //var excelPath = document.FilePath;

            //var relativePath = document.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());

            //var uploadRootTemplate = _uploadRoot
            //    .Replace("{StorageRoot}", _storageRoot)
            //    .Replace("{ClientName}", _clientName);

            //var fullPath = Path.Combine(uploadRootTemplate, relativePath);

            //if (!fullPath.StartsWith(_storageRoot))
            //    throw new SecurityException("Invalid file path");

            //if (!File.Exists(fullPath))
            //    throw new NotFoundException("File not found in storage");

            var relativePath = document.FilePath
    .TrimStart('/', '\\')
    .Replace("\\", "/");

            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var combinedPath = Path.Combine(
                uploadRootTemplate,
                relativePath
            );

            var fullPath = Path.GetFullPath(combinedPath);

            var normalizedStorageRoot = Path.GetFullPath(_storageRoot);

            if (!fullPath.StartsWith(normalizedStorageRoot, StringComparison.Ordinal))
            {
                throw new SecurityException("Invalid file path");
            }

            if (!System.IO.File.Exists(fullPath))
            {
                throw new NotFoundException($"File not found in storage: {fullPath}");
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".xls", ".xlsx", ".xlsb", ".xlsm", ".xltx", ".xltm"
            };

            var extension = Path.GetExtension(document.FileName);

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid Excel file extension/File is not an excel.");
            }
            using var fileStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = application.Workbooks.Open(fileStream);

            foreach (var change in dto.Changes)
            {

                int idx = change.Address.LastIndexOf('!');
                if (idx == -1)
                {
                    response.Failed++;
                    response.FailedDocDetails.Add($"Invalid cell address format: {change.Address}");
                    continue;
                }

                string sheetName = change.Address.Substring(0, idx);
                string cellAddress = change.Address.Substring(idx + 1);

                var worksheet = workbook.Worksheets
                    .FirstOrDefault(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
                if (worksheet == null)
                {
                    response.Failed++;
                    response.FailedDocDetails.Add($"Sheet not found: {sheetName}");
                    continue;
                }

                try
                {
                    var cell = worksheet.Range[cellAddress];
                    //worksheet.Range[cellAddress].Value = change.Value;
                    cell.SetCellValue(change.Value);
                    response.Success++;
                }
                catch (Exception ex)
                {
                    response.Failed++;
                    response.FailedDocDetails.Add($"Cell {change.Address} update failed: {ex.Message}");
                }
            }


            fileStream.SetLength(0);
            workbook.SaveAs(fileStream);
            workbook.Close();

            response.TotalProcessed = dto.Changes.Count;
            return response;

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
            {
                var errorMessage = string.Join(Environment.NewLine, parseResult.Errors);

                throw new ValidationException(
                    $"No records found/Invalid Input format. Errors:{Environment.NewLine}{errorMessage}");
            }


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
                        batchRow.CandidateId = candidate.CandidateId;

                        var alreadyConfirmed =
                        await _context.HrConfirmationBatchRows
                            .AnyAsync(x =>
                            x.CandidateId == candidate.CandidateId
                           && x.IsConfirmed
                            );
                        var existingPendingRows = await _context.HrConfirmationBatchRows
                            .Where(x =>
                                x.CandidateId == candidate.CandidateId &&
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
                    //.ThenInclude(c => c.Request)
                    //    .ThenInclude(r => r.Collection)
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
                // Prevent duplicate insertion of same candidate
                var alreadyExists = await _context.Documents.AnyAsync(d =>
                    d.CandidateId == row.CandidateId &&
                    d.EmployeeId == row.EmployeeId);

                var alreadyExistsEmpId = await _context.Documents.AnyAsync(d =>
                    d.EmployeeId == row.EmployeeId);

                if (alreadyExists || alreadyExistsEmpId)
                {
                    skipped++;
                    continue;
                }

                var document = new Models.Document
                {
                    CandidateId = row.CandidateId,
                    CabinetId = 2,
                    Region = row.Candidate.Region,
                    // Basic onboarding details
                    Designation = row.Designation,
                    Department=row.Department,
                    DOJ = row.DOJ,
                    EmployeeId = row.EmployeeId,
                    DOB = row.DOB,
                    ContactNumber = row.Phone,
                    Name = row.CandidateName,

                    //FileName = string.Empty,
                    //FilePath = string.Empty,

                    Status = "active",
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = userId
                };

                _context.Documents.Add(document);
                row.IsConfirmed = true;
                row.Status = "converted";
                row.ConfirmedAt = DateTime.UtcNow;
                inserted++;
            }

            batch.Status = "processed";

            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(x => x.Id == matchedRows.First().Candidate.Id);
            candidate.IsHired = true;
            //batch.proccessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (inserted > 0 && skipped > 0)
            {
                return new ConfirmedCandidateDto
                {
                    BatchId = batchId,
                    Inserted = inserted,
                    Skipped = skipped,
                    Message = $"Successfully confirmed onboarding of {inserted} candidates and {skipped} candidates are skipped due to technical issues."
                };

            }
            else if (inserted > 0)
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

        public async Task<Models.Document> ConfirmCandidateAsync(ConfirmIndivitualCandidateDto dto, int userId)
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.Id == dto.CandidateId);
            if (candidate == null)
                throw new NotFoundException("Candidate not found");
            if (candidate.IsHired)
                throw new BadRequestException("Candidate is already hired");
            var alreadyExists = await _context.Documents.AnyAsync(d =>
                    d.CandidateId == dto.CandidateId &&
                    d.EmployeeId == dto.EmployeeId);

            var alreadyExistsEmpId = await _context.Documents.AnyAsync(d =>
                   d.EmployeeId == dto.EmployeeId);

            if (alreadyExists || alreadyExistsEmpId)
                throw new BadRequestException("Candidate with same employee ID already exists");
            try
            {

                var document = new Models.Document
                {
                    CandidateId = dto.CandidateId,
                    CabinetId = 2,
                    Region = candidate.Region,
                    // Basic onboarding details
                    Designation = dto.Designation,
                    Department=dto.Department,
                    DOJ = dto.DOJ,
                    EmployeeId = dto.EmployeeId,
                    DOB = candidate.DateOfBirth,
                    ContactNumber = candidate.Phone,
                    Name = candidate.Name,
                    //FileName = string.Empty,
                    //FilePath = string.Empty,
                    Status = "active",
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = userId
                };
                _context.Documents.Add(document);
                candidate.IsHired = true;
                await _context.SaveChangesAsync();
                return document;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error confirming candidate: {ex.Message}");

            }
        }
        //---------------------------------------------------------------------------------------------//


        public async Task<OnboardingDocument> InternalDocumentUploadAsync(InternalUploaddto dto)
        {
            var candidate = await _context.ConfigurationRequestRecipient
                .Include(x=>x.Candidate)
                .FirstOrDefaultAsync(x => x.CandidateId == dto.CandidateId 
                && x.Id == dto.RequestId
                )
                ;

            if (candidate == null)
                throw new NotFoundException("Candidate not found");

            var folderPath = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var safeCandidateName = candidate.Candidate.Name
                .Trim()
                .Replace(" ", "_");

           var orgFolderName = $"{safeCandidateName}_{candidate.Token}";

            var finalPath = Path.Combine(folderPath, orgFolderName);

            if (!Directory.Exists(finalPath))
                Directory.CreateDirectory(finalPath);

            var finalFilePath = Path.Combine(finalPath, dto.FileName);

            File.Move(dto.TempFilePath, finalFilePath, true);

            var dbPath = Path.Combine(orgFolderName, dto.FileName);

            var entity = new OnboardingDocument
            {
                RecipientId = dto.RequestId,
                CandidateId = candidate.CandidateId,
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

            var originalDoc = await _repo.GetOnboardingFilesAsync(dto.Id);
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

            //string tempExtractedPath = Path.GetTempFileName();
            //string tempRemainingPath = Path.GetTempFileName();
            var tempRoot = _tempRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var operationFolder = Path.Combine(
                tempRoot,
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(operationFolder);

            string tempExtractedPath = Path.Combine(
                operationFolder,
                "extract.pdf");

            string tempRemainingPath = Path.Combine(
                operationFolder,
                "remaining.pdf");


            try
            {
                using (var originalPdf =
                    PdfReader.Open(fullPath, PdfDocumentOpenMode.Import))
                    {
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
                }
                // replace original with remaining pages
                //File.Move(tempRemainingPath, fullPath, true);
                File.Copy(tempRemainingPath, fullPath, true);
                File.Delete(tempRemainingPath);

                var extension = Path.GetExtension(originalDoc.FileName);

                var originalName =
                    Path.GetFileNameWithoutExtension(originalDoc.FileName);

                var newFileName =
                    $"{dto.DocumentType}_{originalName}_{Guid.NewGuid():N}{extension}";

                var docType = await _docrepo.GetDocTypeDetailsByNameAsync(dto.DocumentType);

                var uploadDto = new InternalUploaddto
                {
                    RequestId = originalDoc.RecipientId,
                    CandidateId = originalDoc.CandidateId,
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
            //catch
            //{
            //    if (File.Exists(tempExtractedPath))
            //        File.Delete(tempExtractedPath);

            //    if (File.Exists(tempRemainingPath))
            //        File.Delete(tempRemainingPath);

            //    throw;
            //}
            finally
            {
                try
                {
                    if (Directory.Exists(operationFolder))
                    {
                        Directory.Delete(operationFolder, true);
                    }
                }
                catch
                {
                    throw new ServerException("Failed deleting temporary folder.");
                }
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
                "@p_onboarding_type," +
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
                "p_onboarding_type",
                (object?)query.OnboardingType ?? DBNull.Value);

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
                    RequestId = Convert.ToInt64(reader["request_id"]),
                    CandidateName = reader["candidate_name"]?.ToString(),
                    Email = reader["email"]?.ToString(),
                    Region = reader["region"]?.ToString(),
                    OnboardingType = reader["onboarding_type"]?.ToString(),
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
                .GroupBy(x => x.RequestId)
                .Select(g =>
                {
                    var first = g.First();

                    var dto = new OnboardingReportExportDto
                    {
                        CandidateName = first.CandidateName,
                        Email = first.Email,
                        Region = first.Region,
                        OnboardingType = first.OnboardingType,
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
                "OnboardingType",
                "IsHired",
                "CompletionPercent",
                
            
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
                        "OnboardingType" => item.OnboardingType,
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
                   ( dto.Region == null ||
                        r.Request.Collection.Region == dto.Region)
                &&

                (dto.OnboardingType == null ||
                    r.Request.Collection.Type.ToLower() ==
                    dto.OnboardingType.ToLower()));

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
                        x.Status == "pending"),

                    Completed = g.Count(x =>
                        x.Status == "completed"),

                    InProgress = g.Count(x =>
                        x.Status == "inProgress"),

                    Expired = g.Count(x =>
                        x.Status == "expired")
                })
                .FirstOrDefaultAsync();

            return stats ?? new StatusCountResponseDto();
        }

        public async Task<bool> SendLaptopRequestMailAsync(RequestLaptopDto dto, CancellationToken ct = default)
        {

            var candidates = await _context.Candidates
                                  .Where(x =>
                                      dto.CandidateIds.Contains(x.Id) &&
                                      !x.IsLaptopRequestSent)
                                  .ToListAsync(ct);

            if (!candidates.Any())
                throw new BadRequestException("Laptop request already sent.");


            var body = await BuildLaptopRequestBodyAsync(candidates, dto.Message);

            var mailSent = await _emailSender.SendAsync(
                toEmail: dto.To,
                ccEmails: dto.Cc,
                ReplyTo: null,
                UserName: null,
                subject: dto.Subject,
                htmlBody: body,
                ct: ct);

            if (!mailSent)
                return false;
            foreach (var candidate in candidates)
            {
                candidate.IsLaptopRequestSent = true;
                //candidate.LaptopRequestSentAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<string> RemoveCandidateAsync(int candidateId,int userid)
        {
           
            var candidate = await _context.Candidates
                .Include(x=>x.OnboardingDocs)
                .Where(x=>x.Status=="active")
                .FirstOrDefaultAsync(x => x.Id == candidateId);
            if (candidate == null)
                throw new NotFoundException("Candidate not found");
            //if (candidate.IsHired)
            //    throw new BadRequestException("Candidate is already hired");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                candidate.Status = "deleted";
                candidate.DeletedAt = DateTime.UtcNow;
                candidate.DeletedBy = userid;
                foreach (var doc in candidate.OnboardingDocs)
                {
                    doc.Status = "archived";
                }
                await _context.SaveChangesAsync();
                await IsAllCandidatesDeleted(candidateId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return candidate.Name ?? string.Empty;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new ServerException("Failed to remove candidate. Please try again.");
            }
        }



        //-------------------------helpers---------------------------------------

        private async Task IsAllCandidatesDeleted(int candidateId)
        {
            var configReqIds = await _context.ConfigurationRequestRecipient
                .Where(r => r.CandidateId == candidateId)
                .Select(r => r.Request.Id)
                 .Distinct()
                .ToListAsync();
            if (!configReqIds.Any())
                return;

            foreach (var requestId in configReqIds)
            {
                var hasActiveCandidates = await _context.ConfigurationRequestRecipient
                .Include(r => r.Candidate)
                .AnyAsync(r =>
                    r.RequestId == requestId &&
                    r.Candidate.Status == "active");

                if (!hasActiveCandidates)
                {
                    var configRequest = await _context.ConfigurationRequestRecipient
                        .Where(r => r.RequestId == requestId).ToListAsync();

                    foreach (var config in configRequest)
                    {
                        config.Status = "deleted";
                        // config.CancelledAt = DateTime.UtcNow;
                    }
                   
                }
            }

        }
        private async Task<string> BuildLaptopRequestBodyAsync(List<Candidate> candidates, string message)
        {

            var rows = new StringBuilder();

            foreach (var c in candidates)
            {
                rows.Append($@"
            <tr>
                <td style='padding:12px;border:1px solid #ddd;'>{c.Name}</td>
                <td style='padding:12px;border:1px solid #ddd;'>{c.Email}</td>
            </tr>");
            }

            return $@"
            <div style='font-family:Arial, sans-serif; background-color:#f4f6f8; padding:30px;'>
        
        <div style='max-width:700px; background:#ffffff; margin:auto; border-radius:10px; overflow:hidden; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
            
            <div style='background:#00a5b1; color:white; padding:20px;'>
                <h2 style='margin:0;'>Laptop Request</h2>
            </div>

            <div style='padding:30px;'>

                <p>Hello Team,</p>
                 <p>
                    {message}
                </p>
                <p>
                    Kindly arrange laptops for the following candidates.
                </p>
               

                <table style='width:100%; border-collapse:collapse; margin-top:20px;'>

                    <thead>
                        <tr style='background-color:#00a5b1; color:white;'>
                            <th style='padding:12px; text-align:left;'>Candidate Name</th>
                            <th style='padding:12px; text-align:left;'>Email Address</th>
                        </tr>
                    </thead>

                    <tbody>
                        {rows}
                    </tbody>

                </table>

                <p style='margin-top:30px;'>
                    Please process this request at the earliest.
                </p>

                <p>
                    Regards,<br/>
                    Apollo EIPP Vault
                </p>

            </div>

            <div style='background:#f0f0f0; padding:15px; text-align:center; font-size:12px; color:#777;'>
                This is an automated email. Please do not reply.
            </div>

        </div>
    </div>";
        }
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

        private static void ValidatePreOnboardingDocument(OnboardingDocsDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                throw new ValidationException("Token is required");
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Name is required");
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ValidationException("Email is required");
            if (string.IsNullOrWhiteSpace(dto.AdhaarNo))
                throw new ValidationException("AdhaarNo is required");
            if (string.IsNullOrWhiteSpace(dto.PAN))
                throw new ValidationException("PAN is required");
            if (string.IsNullOrWhiteSpace(dto.Phone))
                throw new ValidationException("Phone is required");
            if (dto.Dob == null)
                throw new ValidationException("DOB is required");
            if (dto.Dob > DateTime.UtcNow)
            {
                throw new ValidationException("DOB cannot be in the future");
            }
            if (dto.Dob.HasValue && (DateTime.UtcNow.Year - dto.Dob.Value.Year) < 18)
            {
                throw new ValidationException("DOB indicates age less than 18 years, please verify");
            }
        }

        private async Task ValidateRecipientAsync(ConfigRequestRecipient? recipient, OnboardingDocsDto dto)
        {
            if (recipient == null)
                throw new Exception("Invalid token");

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                dto.Email != recipient.Candidate.Email)
            {
                throw new BadRequestException(
                    "Provided email doesn't match with the one given while applying for the job.");
            }

            if (recipient.Request.ExpiryDate < DateTime.UtcNow)
            {
                recipient.Status = "expired";

                await _context.SaveChangesAsync();

                throw new BadRequestException("Link expired");
            }
        }

        private async Task HandlePreOnboardingUploadAsync(ConfigRequestRecipient recipient, OnboardingDocsDto dto)
        {
            ValidatePreOnboardingDocument(dto);

            recipient.Candidate.Name = dto.Name;
            recipient.Candidate.Phone = dto.Phone;
            recipient.Candidate.DateOfBirth = dto.Dob;
            recipient.Candidate.Adhaar = dto.AdhaarNo;
            recipient.Candidate.PAN = dto.PAN;

            await UploadFilesAsync(
                recipient,
                dto.Files,
                dto.Name!,recipient.Request.Collection.Type);
        }

        private async Task HandlePostOnboardingUploadAsync(ConfigRequestRecipient recipient, OnboardingDocsDto dto)
        {
            var existingPreOboardingStatus = await _repo.GetRecipientReqByCandidateId(recipient.CandidateId);
            if (existingPreOboardingStatus == null)
            {
                throw new BadRequestException(
                    "Pre onboarding request not found");
            }

            if (existingPreOboardingStatus.Status != "completed")
            {
                throw new BadRequestException(
                    "Cannot upload post onboarding documents before completing pre onboarding process");
            }

            await UploadFilesAsync(
                recipient,
                dto.Files,
                recipient.Candidate.Name, recipient.Request.Collection.Type);


        }
        private async Task UploadFilesAsync(ConfigRequestRecipient recipient, List<UploadItemDto> files, string candidateName,string type)
        {
            var assignedDocsId = recipient.Request.Collection.CollectionDocumentTypes.Select(i => i.DocumentTypeId);

            var profilePicDocTypeId = recipient.Request.Collection.CollectionDocumentTypes
                    .FirstOrDefault(x => x.DocumentType.Key == "passport_size_photograph")
                    ?.DocumentTypeId;

            var uploadFolder = BuildFolder(candidateName, recipient.Token, type);



            foreach (var doc in files)
            {
                //var dbPath = string.Empty;
                //var displayFileName = string.Empty;

                if (doc.File == null || doc.File.Length == 0)
                    continue;
                if (!assignedDocsId.Contains(doc.DocumentTypeId))
                {
                    throw new NotFoundException(
                        "Uploaded files includes documents which is not assigned to the recipient");
                }
                
                bool isProfilePic = doc.DocumentTypeId == profilePicDocTypeId;
                string dbPath;
                string displayFileName;

                if (isProfilePic)
                {
                    ValidateProfileImage(doc.File);
                    var profileFolder = BuildProfileImageFolder();
                    var extension = Path.GetExtension(doc.File.FileName);

                    displayFileName =
                        $"{recipient.Candidate.Id}_{Guid.NewGuid():N}{extension}";

                    (dbPath, displayFileName) = await SaveFileAsync(doc.File,profileFolder.FinalFolderPath,null,displayFileName);
                }

                else
                {
                    var originalExtension = Path.GetExtension(doc.File.FileName);

                    // safer doc type label
                    var docTypeLabel = recipient.Request.Collection.CollectionDocumentTypes
                        .First(x => x.DocumentTypeId == doc.DocumentTypeId)
                        .DocumentType.Label
                        .Trim()
                        .Replace(" ", "_");

                    // display filename
                    displayFileName = $"{uploadFolder.SafeCandidateName}_{docTypeLabel}{originalExtension}";


                    (dbPath, displayFileName) = await SaveFileAsync(doc.File,uploadFolder.FinalFolderPath,uploadFolder.OriginalFolderName,displayFileName);
                }

                //Replace if already exists
                var existing = recipient.UploadedDocuments
                    .FirstOrDefault(x => x.DocumentTypeId == doc.DocumentTypeId);

                if (existing != null)
                {
                    DeleteExistingFile(existing.FilePath,existing.DocumentType.Key);
                    existing.FilePath = dbPath;
                    existing.UploadedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.OnboardingHRDocument.Add(new OnboardingDocument
                    {
                        RecipientId = recipient.Id,
                        CandidateId= recipient.Candidate.Id,
                        DocumentTypeId = doc.DocumentTypeId,
                        FilePath = dbPath,
                        FileName = displayFileName,
                        UploadedAt = DateTime.UtcNow,
                        Status = "active",
                        Source = "candidate_upload"
                    });
                }
                
               
            }
            await _context.SaveChangesAsync();

        }
        
        private async Task<(string dbPath, string fileName)> SaveFileAsync(IFormFile file,string folderPath,string? dbFolder,string fileName)
        {
            EnsureDirectoryExists(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            if(dbFolder != null)
            {
                var dbPath = Path.Combine(dbFolder, fileName);
                return (dbPath, fileName);
            }
            return (fileName, fileName);
        }

        private void DeleteExistingFile(string dbPath,string type)
        {
            string rootPath;
            if (string.IsNullOrWhiteSpace(dbPath))
                return;
            if (type == "passport_size_photograph")
            {
                rootPath = _profilePic
           .Replace("{StorageRoot}", _storageRoot)
           .Replace("{ClientName}", _clientName);
            }
            else
            {
                rootPath = _uploadRoot
            .Replace("{StorageRoot}", _storageRoot)
            .Replace("{ClientName}", _clientName);
            }
            var fullPath = Path.Combine(rootPath, dbPath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private void ValidateProfileImage(IFormFile file)
        {
            var allowedExtensions = new[]
            {
                     ".jpg",
                     ".jpeg",
                     ".png",
                     ".webp"
             };

            var extension = Path
                .GetExtension(file.FileName)
                .ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Invalid profile image format");
            }
        }
        private UploadFolderDto BuildProfileImageFolder()
        {
            var folderPath = _profilePic
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            EnsureDirectoryExists(folderPath);

            return new UploadFolderDto
            {
                FinalFolderPath = folderPath
                //,OriginalFolderName = "profile-images"
            };
        }
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        private UploadFolderDto BuildFolder(string candidateName, string token,string type)
        {
            var folderPath = _uploadRoot
                        .Replace("{StorageRoot}", _storageRoot)
                        .Replace("{ClientName}", _clientName);

        var safecandidateName = candidateName.Trim().Replace(" ", "_");
        var orgfolderName = $"{safecandidateName}_{token}";
        
        var finalfolderPath = Path.Combine(folderPath,orgfolderName);

            if (!Directory.Exists(finalfolderPath))
                Directory.CreateDirectory(finalfolderPath);
            return new UploadFolderDto
            {
                SafeCandidateName = safecandidateName,
                FinalFolderPath = finalfolderPath,
                OriginalFolderName = orgfolderName
            };
        }
        private async Task UpdateRecipientStatusAsync(ConfigRequestRecipient recipient)
        {
            var totalDocs = recipient.Request.Collection.CollectionDocumentTypes.Count;

            var uploadedCount = await _repo.GetUploadCount(recipient.Id, recipient.Candidate.Id);

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
        }
        public async Task SendNotificationsAsync(ConfigRequestRecipient recipient)
        {

            if (recipient.Status == "completed")
            {
                await _notificationService.CreateAsync(
                    recipient.Request.CreatedBy,
                    $"Candidate {recipient.Candidate.Name} completed all document uploads"
                );


            var hrUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == recipient.Request.CreatedBy);

                if (hrUser != null && !string.IsNullOrEmpty(hrUser.Email))
                {
                    var subject = "Candidate Document Submission Completed";
                    var link = $"{_baseUrl}";
                    var body = $@"
                        Hello {hrUser.FirstName},

                        The candidate <b>{recipient.Candidate.Name}</b> has successfully uploaded all required documents.

                        <p><b>Candidate Email:</b> {recipient.Candidate.Email}</p>
                        <b>Completion Time:</b> {DateTime.UtcNow}

                        
                        <p><a href='{link}'>Please log in to review the documents.</a></p>


                        <br><p><i>This is a system-generated email. Please do not reply.</i></p></br>
                        Regards,<br/>
                        Apollo EIPP Vault Team
                     ";

                    await _emailSender.SendAsync(hrUser.Email, null, null, subject, body);
                }
            }
            else
            {
                await _notificationService.CreateAsync(
                    recipient.Request.CreatedBy,
                    $"Candidate {recipient.Candidate.Name} uploaded documents, Status - In Progress"
                );
            }

        
        }
        private UploadResultDto BuildUploadResult(ConfigRequestRecipient recipient)
        {

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
    }
}
