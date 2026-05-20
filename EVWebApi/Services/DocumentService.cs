using AutoMapper;

using EVWebApi.Controllers;
using EVWebApi.Data;

using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;

using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Helpers.ExportToExcel;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using EVWebApi.Models;

using Microsoft.EntityFrameworkCore;

using Npgsql;
using PdfSharpCore.Pdf;

using PdfSharpCore.Pdf.IO;

using Syncfusion.XlsIO;


using System.Data;


using System.IO.Compression;

using System.Linq.Dynamic.Core;

using System.Security;

using System.Text.Json;
using static PdfSharpCore.Pdf.PdfDictionary;
using static SkiaSharp.HarfBuzz.SKShaper;
using Document = EVWebApi.Models.Document;
using ValidationException = EVWebApi.Exceptions.ValidationException;


namespace EVWebApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        private readonly IConfigurationRepository _configrepo;
        private readonly IUserRepository _userrepo;
        private readonly IMetadataRepository _metadataRepo;
        private readonly IMetadataReaderFactoryService _metadataReaderFactory;
        public readonly IDocumentGroupingService _docGrpService;
        public readonly IStorageQuotaService _storageQuotaService;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadRoot;
        private readonly string _tempRoot;
        private readonly string _storageRoot;
        private readonly string _clientName;
        private readonly string _versionRoot;
        private readonly string _onboardingPath;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        private readonly IEmailSender _emailSender;
        private readonly IConfigurationService _configService;
        private readonly NpgsqlDataSource _dataSource;
        public readonly IDocVersionService _docVersionService;
        public readonly IDocVersionRepository _docVersionRepo;
        private new readonly AppDbContext _context;
        private readonly ILogger<DocumentController> _logger;

        public DocumentService(IDocumentRepository repo, IMetadataRepository metadataRepo, IMetadataReaderFactoryService metadataReaderFactory,
            IWebHostEnvironment env, IUnitOfWork uow, IMapper mapper, IConfiguration config, IDocumentGroupingService docGrpService, NpgsqlDataSource dataSource
            , IEmailSender emailSender, IUserRepository userrepo, IStorageQuotaService storageQuotaService, IDocVersionService docVersionService,
            IDocVersionRepository docVersionRepository, AppDbContext context, ILogger<DocumentController> logger, IConfigurationService configService, IConfigurationRepository configrepo  )
        {
            _repo = repo;
            _metadataRepo = metadataRepo;
            _metadataReaderFactory = metadataReaderFactory;
            _env = env;
            _uow = uow;
            _mapper = mapper;
            _context = context;
            _uploadRoot = config["DocumentSettings:UploadPath"];
            _storageRoot = config["DocumentSettings:StorageRoot"];
            _tempRoot = config["DocumentSettings:TempPath"];
            _versionRoot = config["DocumentSettings:VersionPath"];
            _clientName = config["DocumentSettings:ClientName"];
            _onboardingPath = config["DocumentSettings:OnboardingPath"];


            _docGrpService = docGrpService;
            _dataSource = dataSource;
            _emailSender = emailSender;
            _userrepo = userrepo;
            _storageQuotaService = storageQuotaService;
            _docVersionService = docVersionService;
            _docVersionRepo = docVersionRepository;
            _logger = logger;
            _configService = configService;
            _configrepo = configrepo;
        }

        // ---------------------- SINGLE UPLOAD ----------------------
        //public async Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto, int? currentuserid)
        //{
        //    if (dto.File == null)
        //        throw new BadRequestException("File is required");

        //    var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
        //    if (cabinet == null)
        //        throw new Exception("Invalid CabinetId");
        //    await _storageQuotaService.ValidateAndConsumeStorage(dto.File.Length);
        //    DocumentTypes? docType = null;
        //    if (!string.IsNullOrWhiteSpace(dto.DocumentType))
        //    {
        //        docType = await _uow.Documents.GetOrCreateDocLabelAsync(dto.DocumentType);
        //    }

        //    // string storageRoot =Path.Combine(_env.WebRootPath, "storage/Uploads");
        //    string folderName = cabinet.CabinetName;
        //    string basePath = _uploadRoot;
        //    string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
        //    string datePath = Path.Combine(basePath, dateFolder);
        //    string cabinetFolder = Path.Combine(datePath, folderName);



        //    if (!Directory.Exists(cabinetFolder))
        //        Directory.CreateDirectory(cabinetFolder);

        //    // Versioning logic
        //    int version = await _repo.GetLatestVersion(dto.CabinetId, dto.File.FileName) + 1;

        //    // Create unique filename
        //    string fileName = $"{Path.GetFileNameWithoutExtension(dto.File.FileName)}_v{version}{Path.GetExtension(dto.File.FileName)}";
        //    string fullPath = Path.Combine(cabinetFolder, fileName);

        //    // Save file physically
        //    using (var stream = new FileStream(fullPath, FileMode.Create))
        //    {
        //        await dto.File.CopyToAsync(stream);
        //    }

        //    // Save to DB
        //    var doc = await _repo.CreateDocument(new Document
        //    {
        //        CabinetId = dto.CabinetId,
        //        FileName = fileName,
        //        //FilePath = $@"\storage\Uploads\{dateFolder}\{folderName}\{fileName}",
        //        FilePath = $@"\{dateFolder}\{folderName}\{fileName}",
        //        UploadedBy = currentuserid,
        //        Version = version,
        //        Status = "active",
        //        UploadedAt = DateTime.UtcNow,
        //        DocumentTypeId = docType?.Id,
        //        DocumentType = null,
        //        InvoiceNumber = dto.InvoiceNumber,
        //        VendorNumber = dto.VendorNumber,
        //        InvoiceDate = dto.InvoiceDate,
        //        Amount = dto.Amount,
        //        GST = dto.GST,
        //        StatementDate = dto.StatementDate,
        //        PaidAmount = dto.PaidAmount,
        //        Department = dto.Department,
        //        Designation = dto.Designation,
        //        Name = dto.Name,
        //        EmployeeId = dto.EmployeeId,
        //        PoNumber = dto.PoNumber,
        //        ContactNumber = dto.ContactNumber,
        //        DOB = dto.DOB,
        //        DOJ = dto.DOJ,
        //        CheckNumber = dto.CheckNumber
        //    });
        //    await _uow.CompleteAsync();
        //    if (doc == null)
        //        throw new ServerException("Failed to save document");
        //    return _mapper.Map<DocumentResponseDto>(doc);
        //}


        // ---------------------- GET DOCUMENT BY Doc Id ----------------------
        public async Task<DocumentResponseDto> GetDocument(int id)
        {
            var doc = await _repo.GetDocument(id);

            if (doc == null)
                throw new NotFoundException("Document not found");

            var metadata = await _metadataRepo.GetMetadataByDocumentId(id);
            return _mapper.Map<DocumentResponseDto>(doc);

        }
        //--------------------- GET BY Cabinet ID --------------------

        //-------filter helper method--------//
        private IQueryable<Document> FilteredQuery(int cabinetId, DocumentQueryParameters query, int? docTypeId)
        {
            var docQuery = _uow.Documents.Query()
                .Where(d => d.CabinetId == cabinetId);

            // status for getting archive or default active
            if (string.IsNullOrWhiteSpace(query.Status))
            {
                docQuery = docQuery.Where(d => d.Status == "active");
            }
            else
            {
                switch (query.Status.ToLower())
                {
                    case "active":
                        docQuery = docQuery.Where(d => d.Status == "active");
                        break;

                    case "archived":
                        docQuery = docQuery.Where(d => d.Status == "archived");
                        break;

                    default:
                        docQuery = docQuery.Where(d => d.Status == "active");
                        break;
                }
            }
            //region
            if (!string.IsNullOrWhiteSpace(query.Region))
                docQuery = docQuery.Where(d => d.Region == query.Region);

            //Document Type
            //if (docTypeId.HasValue)
            //{

            //    docQuery = docQuery.Where(d => d.DocumentTypeId == docTypeId.Value);

            //}

     
            if (docTypeId.HasValue)
            {
                docQuery = docQuery.Where(d =>

                    // OLD DOCUMENTS
                    (d.CandidateId == null &&
                     d.DocumentTypeId == docTypeId.Value)

                    ||

                    // NEW ONBOARDING DOCUMENTS
                    (d.CandidateId != null &&
                     _context.OnboardingHRDocument.Any(o =>
                         o.CandidateId == d.CandidateId &&
                         o.DocumentTypeId == docTypeId.Value))
                )
                    .GroupBy(d => d.CandidateId ?? d.DocumentId)
                    .Select(g => g.First());
                
            }
            // name
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.Name.ToLower().StartsWith(query.Name.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.Name.ToLower().Contains(query.Name.ToLower()));
            }
            //InvoiceNumber
            if (!string.IsNullOrWhiteSpace(query.InvoiceNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.InvoiceNumber.ToLower().StartsWith(query.InvoiceNumber.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.InvoiceNumber.ToLower().Contains(query.InvoiceNumber.ToLower()));
            }
            //ManufactureId
            if (!string.IsNullOrWhiteSpace(query.LoginName))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.LoginName.ToLower().StartsWith(query.LoginName.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.LoginName.ToLower().Contains(query.LoginName.ToLower()));
            }
            //Login ID
            if (!string.IsNullOrWhiteSpace(query.LoginId))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.LoginId.ToLower().StartsWith(query.LoginId.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.LoginId.ToLower().Contains(query.LoginId.ToLower()));
            }
            //GST
            if (query.ManufactureId.HasValue)
            {
                var gstStr = query.ManufactureId.Value.ToString();

                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                {
                    docQuery = docQuery.Where(d => d.ManufactureId.ToString().StartsWith(gstStr));
                }
                else
                {
                    docQuery = docQuery.Where(d => d.ManufactureId.ToString().Contains(gstStr));
                }
            }
            //Period
            if (!string.IsNullOrWhiteSpace(query.Period))
            {
                //var formats = new[] { "MMM yyyy", "MMMM yyyy" };

                //if (DateTime.TryParseExact(
                //        query.Period.Value.ToString("MMM yyyy"),
                //        formats,
                //        CultureInfo.InvariantCulture,
                //        DateTimeStyles.None,
                //        out var parsedDate))
                //{
                //    var period = DateOnly.FromDateTime(parsedDate);

                var period = DateFormatterHelper.ParsePeriod(query.Period);
                docQuery = docQuery.Where(d => d.Period == period);
            }
           
            
            //EMP ID
            if (!string.IsNullOrWhiteSpace(query.EmployeeId))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.EmployeeId.ToLower().StartsWith(query.EmployeeId.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.EmployeeId.ToLower().Contains(query.EmployeeId.ToLower()));
            }
            //designation 
            if (!string.IsNullOrWhiteSpace(query.Designation))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.Designation.ToLower().StartsWith(query.Designation.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.Designation.ToLower().Contains(query.Designation.ToLower()));
            }
            //CONTACT NO
            if (!string.IsNullOrWhiteSpace(query.ContactNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.ContactNumber.StartsWith(query.ContactNumber));
                else
                    docQuery = docQuery.Where(d => d.ContactNumber.Contains(query.ContactNumber));
            }

            // AMOUNT 

            if (query.Amount.HasValue)
            {
                switch (query.AmountType)
                {
                    case AmountType.greater:
                        docQuery = docQuery.Where(d => d.Amount > query.Amount.Value);
                        break;
                    case AmountType.less:
                        docQuery = docQuery.Where(d => d.Amount < query.Amount.Value);
                        break;
                    case AmountType.between:
                        if (query.Amount.HasValue && query.AmountTo.HasValue)
                        {
                            var min = query.Amount.Value;
                            var max = query.AmountTo.Value;

                            docQuery = docQuery.Where(d => d.Amount >= min && d.Amount <= max);
                        }
                        break;
                    case AmountType.equal:
                    default:
                        docQuery = docQuery.Where(d => d.Amount == query.Amount.Value);
                        break;
                }
            }
            //PaidAmount
            if (query.PaidAmount.HasValue)
            {
                switch (query.AmountType)
                {
                    case AmountType.greater:
                        docQuery = docQuery.Where(d => d.PaidAmount > query.PaidAmount.Value);
                        break;
                    case AmountType.less:
                        docQuery = docQuery.Where(d => d.PaidAmount < query.PaidAmount.Value);
                        break;
                    case AmountType.between:
                        if (query.PaidAmount.HasValue && query.PaidAmountTo.HasValue)
                        {
                            var min = query.PaidAmount.Value;
                            var max = query.PaidAmountTo.Value;

                            docQuery = docQuery.Where(d => d.PaidAmount >= min && d.PaidAmount <= max);
                        }
                        break;
                    case AmountType.equal:
                    default:
                        docQuery = docQuery.Where(d => d.PaidAmount == query.PaidAmount.Value);
                        break;
                }
            }

            // InvoiceDate

            if (query.InvoiceDate.HasValue)
            {
                switch (query.DateType)
                {
                    case DateType.after:
                        docQuery = docQuery.Where(d => d.InvoiceDate >= query.InvoiceDate.Value);
                        break;
                    case DateType.before:
                        docQuery = docQuery.Where(d => d.InvoiceDate <= query.InvoiceDate.Value);
                        break;
                    case DateType.between:
                        if (query.InvoiceDate.HasValue && query.InvoiceDateTo.HasValue)
                        {
                            var d1 = query.InvoiceDate.Value.Date;
                            var d2 = query.InvoiceDateTo.Value.Date;

                            docQuery = docQuery.Where(d =>
                                d.InvoiceDate >= d1 && d.InvoiceDate <= d2
                            );
                        }
                        break;

                    case DateType.on:
                    default:
                        docQuery = docQuery.Where(d => d.InvoiceDate == query.InvoiceDate.Value);

                        break;
                }
            }
            //DOJ
            if (query.DOJ.HasValue)
            {
                switch (query.DateType)
                {
                    case DateType.after:
                        docQuery = docQuery.Where(d => d.DOJ >= query.DOJ.Value);
                        break;
                    case DateType.before:
                        docQuery = docQuery.Where(d => d.DOJ <= query.DOJ.Value);
                        break;
                    case DateType.between:
                        if (query.DOJ.HasValue && query.DOJDateTo.HasValue)
                        {
                            var d1 = query.DOJ.Value.Date;
                            var d2 = query.DOJDateTo.Value.Date;

                            docQuery = docQuery.Where(d =>
                                d.DOJ >= d1 && d.DOJ <= d2
                            );
                        }
                        break;
                    case DateType.on:
                    default:
                        docQuery = docQuery.Where(d => d.DOJ == query.DOJ.Value);
                        break;
                }
            }
            return docQuery;
        }

        public async Task<PagedResponse<DocumentResponseDto>> GetDocumentsByCabinetId(int cabinetId, DocumentQueryParameters query)
        {

            int? docTypeId = null;

            if (!string.IsNullOrWhiteSpace(query.DocType))
            {
               
                var docType = await _uow.Documents.GetDocTypeDetailsByNameAsync(query.DocType);
                docTypeId = docType.Id;
            }

            var docQuery = FilteredQuery(cabinetId, query, docTypeId);

            var totalRecords = await docQuery.CountAsync();

            // If pageSize is invalid, normalize it
            if (query.PageSize <= 0)
                query.PageSize = 10;

            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalRecords / (double)query.PageSize);

            // Normalize pageNumber
            if (query.PageNumber <= 0)
                query.PageNumber = 1;

            if (query.PageNumber > totalPages && totalPages > 0)
                query.PageNumber = totalPages;

            var pagedDocs = await docQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // MAP TO DTO
            var docDtos = _mapper.Map<List<DocumentResponseDto>>(pagedDocs);

            return new PagedResponse<DocumentResponseDto>
            {
                Data = docDtos,
                TotalRecords = totalRecords,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<GroupedPaginationResponse<GroupedDocResponseDTO>> GetGroupedDocuments(int cabinetId, DocumentQueryParameters query)
        {
            int? docTypeId = null;

            if (!string.IsNullOrWhiteSpace(query.DocType))
            {
                var docType = await _uow.Documents.GetDocTypeDetailsByNameAsync(query.DocType);
                docTypeId = docType.Id;
            }
            var docQuery = FilteredQuery(cabinetId, query, docTypeId);

            var documents = await docQuery.ToListAsync();

            if (!documents.Any())
            {
                return new GroupedPaginationResponse<GroupedDocResponseDTO>
                {
                    Data = new List<GroupedDocResponseDTO>(),
                    TotalRecords = 0,
                    TotalRows = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }


            var groupingKey = await _docGrpService.GetDynamicGroupingKeyAsync(cabinetId, "grouping");
            //fetching onboarding docs basd on candidate id
            var onboardingCandidateIds = documents
                .Where(x =>
                    string.IsNullOrWhiteSpace(x.FileName) &&
                    string.IsNullOrWhiteSpace(x.FilePath) &&
                    x.CandidateId != null && x.Status == "active")
                .Select(x => x.CandidateId!.Value)
                .Distinct()
                .ToList();

            var candidateDocs = await _context.OnboardingHRDocument
                .Include(x => x.DocumentType)
                .Where(x => onboardingCandidateIds.Contains(x.CandidateId) && x.Status == "active")
                .ToListAsync();

            var groupedCandidateDocs = candidateDocs
                .GroupBy(x => x.CandidateId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());

            var result = documents
               .GroupBy(d =>
                   string.Join("||", groupingKey.Select(k =>
                       d.GetType().GetProperty(k)?.GetValue(d)?.ToString() ?? "")))
               .Select(g =>
               {
                   var first = g.First();
                   return new GroupedDocResponseDTO
                   {
                       EmployeeId = first.EmployeeId,
                       Name = first.Name,
                       ContactNumber = first.ContactNumber,
                       Designation = first.Designation,
                       DOB = first.DOB,
                       DOJ = first.DOJ,
                       Region = first.Region,
                       InvoiceNumber = first.InvoiceNumber,
                       InvoiceDate = first.InvoiceDate,
                       //VendorNumber = first.VendorNumber,
                       StatementDate = first.StatementDate,
                       //PoNumber = first.PoNumber,
                       Amount = first.Amount,
                       PaidAmount = first.PaidAmount,
                      // GST = first.GST,
                       //CheckNumber = first.CheckNumber,
                       Version =first.Version,
                       Status = first.Status,
                       CabinetId = first.CabinetId,

                       DocumentTypes = g.SelectMany(d =>
                       {
                           // Normal uploaded document

                           if (!string.IsNullOrWhiteSpace(d.FileName) &&
                               !string.IsNullOrWhiteSpace(d.FilePath))
                           {
                               return new List<DocumentChildDDTO>
                                {
                                    new DocumentChildDDTO
                                    {
                                        DocumentId = d.DocumentId,
                                        DocumentType = d.DocumentType?.Label,
                                        NotesCount = d.Notes?.Count ?? 0,
                                        FileName = d.FileName,
                                        FilePath = d.FilePath
                                    }
                                };
                           }

                       
                           // Onboarding documents
                  

                           if (d.CandidateId != null &&
                               groupedCandidateDocs.TryGetValue(
                                   d.CandidateId.Value,
                                   out var uploads))
                           {
                               return uploads
                                .Where(x => !docTypeId.HasValue || x.DocumentTypeId == docTypeId.Value)
                                .Select(u => new DocumentChildDDTO
                               {
                                   //DocumentId = u.Id,
                                   DocumentId = d.DocumentId,
                                   OnboardingDocId = u.Id,
                                   DocumentType = u.DocumentType?.Label,
                                   NotesCount = d.Notes?.Count ?? 0,
                                   FileName = u.FileName,
                                   FilePath = u.FilePath
                               })
                               
                               .ToList();
                           }

                           return Enumerable.Empty<DocumentChildDDTO>();
                       }).ToList()
                   };
               })
               .ToList();


            if (query.PageSize <= 0)
                query.PageSize = 10;

            var totalRecords = documents.Count;
            int totalRows = result.Count;
            int totalPages = (int)Math.Ceiling(totalRows / (double)query.PageSize);


            if (query.PageNumber <= 0)
                query.PageNumber = 1;

            if (query.PageNumber > totalPages && totalPages > 0)
                query.PageNumber = totalPages;

            var pagedDocs = result
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();


            //var docDtos = _mapper.Map<List<GroupedDocResponseDTO>>(pagedDocs);

            return new GroupedPaginationResponse<GroupedDocResponseDTO>
            {
                Data = pagedDocs,
                TotalRecords = totalRecords,
                TotalRows = totalRows,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        // ---------------------- PREVIEW STREAM ----------------------
        //public async Task<DocumentStreamResultDTO?> GetDocumentStream(DocumentRequestDto dto)
        //{
        //    if (dto.DocumentId == null && dto.OnboardingDocId == null)
        //        throw new ArgumentException("Either DocumentId or OnboardingDocId must be provided");


        //    var doc = await _repo.GetDocument(dto.DocumentId.Value);
        //    if (doc == null)
        //        throw new NotFoundException("Document not found");

        //    if (dto.OnboardingDocId != null || (doc.FileName==null && doc.FilePath==null && doc.CandidateId!=null))
        //    {
        //        if(doc.CandidateId!=null)
        //            dto.OnboardingDocId = doc.CandidateId;
        //        var onboardingDoc = await _configService.GetOnboardingDocumentStream(dto.OnboardingDocId.Value);
        //        if (onboardingDoc != null)
        //            return onboardingDoc;
        //    }

        //    var relativePath = doc.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());

        //    var uploadRootTemplate = _uploadRoot
        //        .Replace("{StorageRoot}", _storageRoot)
        //        .Replace("{ClientName}", _clientName);

        //    var fullPath = Path.Combine(uploadRootTemplate, relativePath);

        //    if (!fullPath.StartsWith(_storageRoot))
        //        throw new SecurityException("Invalid file path");

        //    if (!File.Exists(fullPath))
        //        throw new NotFoundException("File not found in storage");

        //    var stream = new FileStream(
        //        fullPath,
        //        FileMode.Open,
        //        FileAccess.Read,
        //        FileShare.Read,
        //        bufferSize: 1024 * 1024,
        //        useAsync: true
        //    );

        //    return new DocumentStreamResultDTO
        //    {
        //        Stream = stream,
        //        FilePath = fullPath,
        //        FileName = doc.FileName
        //    };
        //}

        public async Task<DocumentStreamResultDTO?> GetDocumentStream(DocumentRequestDto dto)
        {
            if (dto.Id <= 0)
                throw new ArgumentException("Invalid Id");

            if (dto.Source == DocumentSourceType.Onboarding)
            {
                var onboardingDoc = await _configService.GetOnboardingDocumentStream(dto.Id);
                if (onboardingDoc == null)
                    throw new NotFoundException("Onboarding document not found");

                return onboardingDoc;
            }

            var doc = await _repo.GetDocument(dto.Id);
            if (doc == null)
                throw new NotFoundException("Document not found");

            var relativePath = doc.FilePath?.TrimStart('/', '\\')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            if (string.IsNullOrEmpty(relativePath))
                throw new NotFoundException("Invalid document path");

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
                FileName = doc.FileName
            };
        }
        // ---------------------- DOWNLOAD ----------------------
        public async Task<DocumentDownloadDto?> GetDocumentForDownload(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null) return null;

            var rootPath = doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var fullPath = Path.Combine(uploadRootTemplate, rootPath);

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            return new DocumentDownloadDto
            {
                FilePath = fullPath,
                FileName = doc.FileName
            };
        }


        //--------------------------FILE EXPLORER-------------------

        public async Task<List<DocumentFileExplorer>> GetFileExplorerDocumentAsync(int id)
        {
            var cabinet = await _uow.Cabinets.GetByIdAsync(id);

            if (cabinet == null)
                throw new NotFoundException("Cabinet not found");

            var files = await _repo.GetFileExplorerAsync(id);
            if (files == null)
                throw new NotFoundException("Files not found in storage");


            return files.Select(d => new DocumentFileExplorer
            {
                DocumentId = d.DocumentId,
                FilePath = Path.GetDirectoryName(d.FilePath)!.Replace("\\", "/"),
                FileName = d.FileName
            }).ToList();
        }


        // ---------------------- ARCHIVE ----------------------
        //public async Task ArchiveDocument(int id)
        //{
        //    await _repo.UpdateStatus(id, "archived");
        //}

        //// ---------------------- RESTORE ----------------------
        //public async Task RestoreDocument(int id)
        //{
        //    await _repo.UpdateStatus(id, "active");
        //}


        // -----------------SINGLE-DELETE--------------------
        public async Task<(int cabinetId, bool status)> DeleteDocument(DocumentRequestDto dto)
        {
            //var doc = await _repo.GetDocument(id);
            //if (doc == null)
            //    throw new NotFoundException("Document not found");
            int cabinetid;


            
            try
            {
                if (dto == null)
                    throw new BadRequestException("Invalid request data.");
                if (dto.Source == DocumentSourceType.Document)
                {
                    var doc = await _repo.DeleteDocument(dto.Id);
                    cabinetid = doc.CabinetId;
                }
                else
                {
                    var doc=await _configrepo.DeleteOnboardingDocument(dto.Id);

                    await IsAllOnboardingDocsArchived(doc.CandidateId);
                    cabinetid = 2;//hardcoding hr cabinet id
                }
                return (cabinetid, true);
            }
            catch(Exception ex)
            { return (0, false); }
        }

        private async Task IsAllOnboardingDocsArchived(int candidateId)
        {
            bool hasActiveDocs =
            await _context.OnboardingHRDocument
            .AnyAsync(x =>
            x.CandidateId == candidateId &&
            x.Status != "archived");

            if (!hasActiveDocs)
            {
                var parentDoc = await _context.Documents
                    .FirstOrDefaultAsync(x =>
                        x.CandidateId == candidateId);

                if (parentDoc != null)
                {
                    parentDoc.Status = "archived";
                }
            }
            await _context.SaveChangesAsync();
        }

        // -----------------MULTI-DELETE--------------------
        public async Task<BatchResponseDTO> DeleteMultipleDocuments(ExportDto dto)
        {
            var summary = new BatchResponseDTO();

            
            foreach (var item in dto.Documents)
            {
                summary.TotalProcessed++;
                try
                {
                    if (item.Source == DocumentSourceType.Document)
                    {
                        await _repo.DeleteDocument(item.Id);
                        summary.Success++;
                    }
                    else
                    {
                        var doc=await _configrepo.DeleteOnboardingDocument(item.Id);
                        await IsAllOnboardingDocsArchived(doc.CandidateId);
                        summary.Success++;
                    }
                  
                   
                }
                catch (Exception ex)
                {
                    summary.FailedDocDetails.Add($"Error deleting Document ID {item.Id}: {ex.Message}");
                    summary.Failed++;
                }

            }
            return summary;
        }
        //-------------------- Export Merged PDF-----------------------

        //public async Task<Stream?> GetMergedDocumentStream(List<int> documentIds)
        //{
        //    var outputStream = new MemoryStream();

        //    using (var outputDocument = new PdfSharpCore.Pdf.PdfDocument())
        //    {
        //        foreach (var id in documentIds)
        //        {
        //            var pdfStream = await GetDocumentStream(id);
        //            if (pdfStream == null)
        //                throw new NotFoundException($"Document not found: {id}");

        //            using (pdfStream.Stream)
        //            using (var inputDocument = PdfSharpCore.Pdf.IO.PdfReader.Open(
        //                pdfStream.Stream,
        //                PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import))
        //            {
        //                for (int i = 0; i < inputDocument.PageCount; i++)
        //                {
        //                    outputDocument.AddPage(inputDocument.Pages[i]);
        //                }
        //            }
        //        }

        //        outputDocument.Save(outputStream, false);
        //    }

        //    outputStream.Position = 0;
        //    return outputStream;
        //}
        public async Task<Stream?> GetMergedDocumentStream(ExportDto dto)
        {
            var outputStream = new MemoryStream();

            using (var outputDocument = new PdfSharpCore.Pdf.PdfDocument())
            {
                foreach (var item in dto.Documents)
                {
                    DocumentStreamResultDTO? pdfStream = null;

                    if (item.Source == DocumentSourceType.Document)
                    {
                        pdfStream = await GetDocumentStream(item);
                    }
                    else if (item.Source == DocumentSourceType.Onboarding)
                    {
                        pdfStream = await _configService.GetOnboardingDocumentStream(item.Id);
                    }

                    if (pdfStream == null)
                        throw new NotFoundException($"Document not found: {item.Id}");

                    using (pdfStream.Stream)
                    using (var inputDocument = PdfSharpCore.Pdf.IO.PdfReader.Open(
                        pdfStream.Stream,
                        PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import))
                    {
                        for (int i = 0; i < inputDocument.PageCount; i++)
                        {
                            outputDocument.AddPage(inputDocument.Pages[i]);
                        }
                    }
                }

                outputDocument.Save(outputStream, false);
            }

            outputStream.Position = 0;
            return outputStream;
        }

        //--------------------- EXPORT TO ZIP File-------------------

        public async Task<(Stream ZipStream, string ZipFileName)> GetZIPFile(ExportDto dto)
        {
            var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in dto.Documents)
                {
                    DocumentStreamResultDTO? pdfStream = null;
                   // var originalFileName=default(string);
                    if (item.Source == DocumentSourceType.Document)
                    {
                        pdfStream = await GetDocumentStream(item);
                        //originalFileName = await _repo.GetDocumentName(item.Id);
                    }
                    else if (item.Source == DocumentSourceType.Onboarding)
                    {
                        pdfStream = await _configService.GetOnboardingDocumentStream(item.Id);
                        //originalFileName = await _configrepo.GetOnboardingFileNameById(item.Id);
                        
                    }

                    if (pdfStream == null)
                        throw new NotFoundException($"Document not found: {item.Id}");

                    // Generate file name
                   

                    // Generate unique filename for ZIP: originalname_id.pdf
                    //var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(pdfStream.FileName);
                    var extension = Path.GetExtension(pdfStream.FileName);
                    var FileName = $"{fileNameWithoutExt}_{item.Id}{extension}";

                    var zipEntry = archive.CreateEntry(FileName, CompressionLevel.Fastest);
                    using (var entryStream = zipEntry.Open())
                    {
                        await pdfStream.Stream.CopyToAsync(entryStream);
                    }
                }
            }

            memoryStream.Position = 0;
            var cabinetName = await _uow.Cabinets.GetCabinetNameAsync(dto.CabinetId) ?? "UnknownCabinet";
            var safeCabinetName = string.Concat(cabinetName.Split(Path.GetInvalidFileNameChars()));
            var zipFileName = $"{safeCabinetName}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.zip";

            return (memoryStream, zipFileName);

        }

        //-------------------------export to excel-------------------

        private List<Document> GroupDocumentsForExport(
            List<Document> documents,
            List<string> groupingKey)
        {
            return documents
                .GroupBy(d =>
                    string.Join("||", groupingKey.Select(k =>
                        d.GetType().GetProperty(k)?.GetValue(d)?.ToString() ?? "")))
                .Select(g =>
                {
                    var first = g.First();

                    first.DocType = string.Join(", ",
                        g.Select(d => d.DocumentType?.Label)
                         .Where(x => !string.IsNullOrEmpty(x))
                         .Distinct());

                    return first;
                })
                .ToList();
        }
        public async Task<(byte[] Excel,string FileName)> GetExportExcel(ExportExcelDocDto dto)
        {
            var documents = await _repo.ExcelExportQuery(dto);
            var keys = await _docGrpService.GetDynamicGroupingKeyAsync(dto.CabinetId, "grouping");

            var hasDocType = documents.Any(d => d.DocumentTypeId != null);
            var needGrouping = documents
                .GroupBy(d => string.Join("||", keys.Select(k =>
                 d.GetType().GetProperty(k)?.GetValue(d)?.ToString() ?? "")))
                .Any(g => g.Count() > 1);

            if (needGrouping)
            {
                documents = GroupDocumentsForExport(documents, keys);
            }
            else if (hasDocType)
            {
                foreach (var doc in documents)
                {
                    doc.DocType = doc.DocumentType?.Label;
                }
            }
            if (hasDocType && !keys.Contains("DocType"))
            {
                keys.Add("DocType");
            }

            using var excelEngine = new ExcelEngine();
            var app = excelEngine.Excel;
            app.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = app.Workbooks.Create(1);
            var sheet = workbook.Worksheets[0];
            sheet.Name = "Documents";

            // header
         
            for (int col = 0; col < keys.Count; col++)
            {
                sheet.Range[1, col + 1].Text = keys[col];
            }

            sheet.Range[1, 1, 1, keys.Count].CellStyle.Font.Bold = true;


            int row = 2;
            foreach (var doc in documents)
            {
                for (int col = 0; col < keys.Count; col++)
                {
                    var value = DocumentColumnHelper.GetColumnValue(doc,keys[col]);

                    sheet.Range[row, col + 1].Text =
                        value is DateTime dt
                            ? dt.ToString("dd/MM/yyyy")
                            : value?.ToString();
                }
                row++;
            }

            sheet.UsedRange.AutofitColumns();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var cabinetName = await _uow.Cabinets.GetCabinetNameAsync(dto.CabinetId) ?? "UnknownCabinet";
            var safeCabinetName = string.Concat(cabinetName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeCabinetName}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx";

            return (stream.ToArray(), fileName);
        }

        //--------------------- EDIT ---------------------------------

        public async Task<DocumentResponseDto> UpdateDocumentAsync(int id, UpdateDocumentDto dto, int? userId)
        {
            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            if (cabinet == null)
                throw new BadRequestException("Invalid CabinetId");

            var document = await _uow.Documents.GetByIdAsync(id);
            if (document == null || document.Status == "inactive")
                throw new NotFoundException($"Document with id {id} not found");
            //checking for lock
            var userLock = await _docVersionService.CheckDocLockValidityAsync(document.DocumentId, userId);
            //if(userLock == null)
            //    throw new ConflictException("Document is currently being edited by another user/No active lock for User.");
            using var transaction = await _uow.BeginTransactionAsync();
            try
            {

                //versioning
                var latestVersion = await _repo.GetLatestVersion(id) + 1;

                if (dto.CabinetId != 0)
                    document.CabinetId = dto.CabinetId;

                if (!string.IsNullOrWhiteSpace(dto.FileName))
                    document.FileName = dto.FileName;

                if (!string.IsNullOrWhiteSpace(dto.FilePath))
                    document.FilePath = dto.FilePath;


                if (dto.InvoiceNumber != null) document.InvoiceNumber = dto.InvoiceNumber;
                if (dto.ManufactureId != null) document.ManufactureId = dto.ManufactureId;
                //if (dto.LoginId != null) document.LoginId = dto.LoginId;
                if (dto.EmployeeId != null) document.EmployeeId = dto.EmployeeId;
                if (dto.Name != null) document.Name = dto.Name;
                if (dto.ContactNumber != null) document.ContactNumber = dto.ContactNumber;
                if (dto.Designation != null) document.Designation = dto.Designation;
                if (dto.Department != null) document.Department = dto.Department;
                if (dto.Region != null) document.Region = dto.Region;
                if (dto.InvoiceDate.HasValue) document.InvoiceDate = dto.InvoiceDate.Value;
                if (dto.StatementDate.HasValue) document.StatementDate = dto.StatementDate.Value;
                if (dto.DOJ.HasValue) document.DOJ = dto.DOJ.Value;
                if (dto.DOB.HasValue) document.DOB = dto.DOB.Value;
                if (dto.Amount.HasValue) document.Amount = dto.Amount.Value;
                if (dto.Period != null) document.Period = DateFormatterHelper.ParsePeriod(dto.Period);



                //if (dto.LoginName != null) document.LoginName = dto.LoginName;
                if (dto.Remarks != null) document.Remarks = dto.Remarks;
                if (dto.PaidAmount.HasValue) document.PaidAmount = dto.PaidAmount.Value;
                document.Version = latestVersion;
                DocumentTypes? docType = null;
                if (!string.IsNullOrWhiteSpace(dto.DocumentType))
                {
                    docType = await _uow.Documents.GetDocTypeDetailsByNameAsync(dto.DocumentType);
                    document.DocumentTypeId = docType.Id;
                    //document.DocumentType = null;
                }

                _uow.Documents.Update(document);
                var versionEntity = _mapper.Map<DocumentVersion>(document);//need to check
                var version=await _docVersionService.CreateVersionAsync(versionEntity, latestVersion);

                document.Version = version.VersionId;
                version.Action = "modified";

                await _uow.CompleteAsync();
                await transaction.CommitAsync();
                //await EnforceVersionLimit(document.DocumentId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            finally
            {
                if (userLock != null && userLock.LockedBy == userId)
                {
                    _docVersionRepo.RemoveLock(userLock);
                    await _uow.CompleteAsync();
                }
            }

            return _mapper.Map<DocumentResponseDto>(document);
        }
        


        //############################NOTES#################################



        //--------------------GET NOTES BY DOC ID------------------------

        public async Task<List<NotesDto>> GetDocumentWithNotesAsync(int id)
        {
            var document = await _uow.Documents.GetByIdAsync(id);

            if (document == null || document.Status == "inactive")
                throw new NotFoundException($"Document with id {id} not found");

            var doc = await _repo.GetDocumentWithNotesAsync(id);


            return doc;
        }
        //create note
        public async Task<NotesDto> CreateNoteAsync(NoteCreateDto dto, string CurrentUsername)
        {
            var note = new Notes
            {
                NoteText = dto.NoteText,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUsername,
                DocumentId = dto.DocumentId
            };
            await _uow.Documents.AddNoteAsync(note);
            await _uow.CompleteAsync();
            return _mapper.Map<NotesDto>(note);
        }

        public async Task<NotesDto> UpdateNoteAsync(NoteUpdateDto dto)
        {

            var note = await _uow.Documents.GetNoteByIdAsync(dto.NoteId);
            if (note == null)
                throw new ConflictException($"Note with id {dto.NoteId}  not found.");


            if (!string.IsNullOrWhiteSpace(dto.NoteText)) note.NoteText = dto.NoteText;

            _uow.Documents.UpdateNote(note);
            await _uow.CompleteAsync();
            return _mapper.Map<NotesDto>(note);
        }

        public async Task<string> DeleteNoteAsync(long id)
        {
            var note = await _uow.Documents.GetNoteByIdAsync(id);
            if (note == null)
                throw new NotFoundException("Note not found");
            string noteText = note.NoteText;
            _uow.Documents.DeleteNote(note);
            await _uow.CompleteAsync();
            return noteText;
        }

        //-----------------GET DOC TYPE------------------------

        public async Task<List<DocTypeCreateDto>> GetDocTypeAsync()
        {
            var doctype = await _uow.Documents.GetDocTypesAsync();
            if (doctype == null)
                throw new NotFoundException("Document types  not found");
            return doctype;
        }


        // ----------------- BATCH UPLOAD------------------------

        public async Task<BatchResponseDTO> BatchUploadDocuments(BatchUploadDTO dto, int? currentuserid, string? username, string? fullname)
        {
            if (dto.MetadataFile == null || dto.Files == null || dto.Files.Count == 0)
                throw new ArgumentException("CSV or PDF files are missing.");

            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId)
                ?? throw new Exception("Invalid CabinetId");

            var extension = Path.GetExtension(dto.MetadataFile.FileName).ToLower();
            var reader = _metadataReaderFactory.GetReader(extension);

            var metadataResult = await reader.ReadAsync<DocumentMetadatadto>(dto.MetadataFile);

            if (metadataResult.TotalRecords == 0 || metadataResult.Records.Count == 0)
                throw new ArgumentException("Unable to read metadata file");
            
            //validation
            var requiredHeaders = await _docGrpService.GetDynamicGroupingKeyAsync(dto.CabinetId,"upload");
            if (requiredHeaders != null)
            {
                requiredHeaders.Add("FileName");
                if (cabinet.CabinetId == 2)
                    requiredHeaders.Add("DocumentType");
            }
            var missingHeaders = requiredHeaders
                .Except(metadataResult.Headers,
                 StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingHeaders.Any())
            {
                throw new ValidationException(
                    $"Missing required columns: {string.Join(", ", missingHeaders)}");
            }



            var summary = new BatchResponseDTO();
            summary.Failed += metadataResult.Errors.Count;
            summary.FailedDocDetails.AddRange(metadataResult.Errors);

            var processedFiles = new List<string>();
            var batchKeys = new HashSet<string>();

            int totalCount = 0;

            
            var planDetails = await _storageQuotaService.GetPlanUsageAsync();
            decimal remainingBytes = planDetails.RemaianingBytes;

            
            var existingDocs = await _repo.GetDocumentsForDuplicateCheck(dto.CabinetId);
            CabinetDuplicateRulesHelper.TryGetRules(dto.CabinetId, out var fields);

            var existingDocMap = existingDocs
                .GroupBy(d => _repo.GenerateDuplicateKeyFromDocument(d, fields))
                .ToDictionary(g => g.Key, g => g.First());

            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            string CabName = cabinet.CabinetName;
            string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string finalFolder = Path.Combine(uploadRootTemplate, CabName, dateFolder);

            if (!Directory.Exists(finalFolder))
                Directory.CreateDirectory(finalFolder);

            var docTypeCache = new Dictionary<string, DocumentTypes>();

            foreach (var record in metadataResult.Records)
            {
                totalCount++;

                try
                {
                    var physicalFile = dto.Files.FirstOrDefault(f =>
                        f.FileName.Equals(record.FileName, StringComparison.OrdinalIgnoreCase));

                    if (physicalFile == null)
                    {
                        summary.Failed++;
                        summary.FailedDocDetails.Add($"Row {totalCount}: File not found {record.FileName}");
                        continue;
                    }

                    
                    if (physicalFile.Length > remainingBytes)
                    {
                        summary.Failed++;
                        summary.FailedDocDetails.Add($"Quota exceeded: {record.FileName}");
                        continue;
                    }

                    var key = _repo.GenerateDuplicateKeyFromRecord(record, fields);

                    //to chechk db duplicate existence
                    if (existingDocMap.ContainsKey(key))
                    {
                        summary.Failed++;
                        summary.FailedDocDetails.Add($" {record.FileName} : Skipped upload as duplicate record with same index data found in system.");
                        continue;
                    }
                    // to chechk is repetateion /duplicate row in csv
                    if (batchKeys.Contains(key))
                    {
                        summary.Failed++;
                        summary.FailedDocDetails.Add($" {record.FileName}:Duplicate row found within the batch");
                        continue;
                    }

                    batchKeys.Add(key);
                    
                    var ext = Path.GetExtension(record.FileName);
                    var baseName = Path.GetFileNameWithoutExtension(record.FileName);
                    string fileName = $"{baseName}_v1{ext}";
                    string fullPath = Path.Combine(finalFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await physicalFile.CopyToAsync(stream);
                    }

                    processedFiles.Add(fullPath);

                  
                    DocumentTypes? docType = null;
                    if (!string.IsNullOrWhiteSpace(record.DocumentType))
                    {
                        if (!docTypeCache.TryGetValue(record.DocumentType, out docType))
                        {

                            docType = await _uow.Documents.GetDocTypeDetailsByNameAsync(record.DocumentType);///Now others won't work...for others need pre or post
                            docTypeCache[record.DocumentType] = docType;
                        }
                    }

                    if (record.DOB.HasValue && record.DOJ.HasValue && record.DOJ < record.DOB)
                    {
                        throw new ValidationException("DOJ cannot be earlier than DOB");
                    }
                    if (record.DOJ > DateTime.UtcNow || record.DOB > DateTime.UtcNow)
                    {
                        throw new ValidationException("DOJ and DOB cannot be in the future");
                    }
                    if (record.DOB.HasValue && (DateTime.UtcNow.Year - record.DOB.Value.Year) < 18)
                    {
                        throw new ValidationException("DOB indicates age less than 18 years, please verify");
                    }

                    var document = new Document
                    {
                        CabinetId = dto.CabinetId,
                        FileName = fileName,
                        FilePath = $@"{CabName}\{dateFolder}\{fileName}",
                        Status = "active",
                        UploadedBy = currentuserid,
                        UploadedAt = DateTime.UtcNow,
                        DocumentTypeId = docType?.Id,
                        Region=record.Region,
                        InvoiceNumber = record.InvoiceNumber,
                        
                        InvoiceDate = record.InvoiceDate,
                        Amount = record.Amount,
                        
                        StatementDate = record.StatementDate,
                        PaidAmount = record.PaidAmount,
                        Department = record.Department,
                        Designation = record.Designation,
                        Name = record.Name,
                        EmployeeId = record.EmployeeId,
                        
                        ContactNumber = record.ContactNumber,
                        DOB = record.DOB,
                        DOJ = record.DOJ,

                        ManufactureId = record.ManufactureId,
                        Period = DateFormatterHelper.ParsePeriod(record.Period),
                        LoginName = fullname,
                        LoginId =username,
                        Remarks = record.Remarks

                    };

                    _repo.AddDocumentRange(document);
                    await _uow.CompleteAsync(); // needed for DocumentId

                    
                    var versionTemp = _versionRoot
                        .Replace("{StorageRoot}", _storageRoot)
                        .Replace("{ClientName}", _clientName);

                    var versionFolder = Path.Combine(versionTemp, document.DocumentId.ToString());
                    Directory.CreateDirectory(versionFolder);

                    var versionPath = Path.Combine(versionFolder, document.FileName);
                    File.Copy(fullPath, versionPath);

                    var versionEntity = _mapper.Map<DocumentVersion>(document);
                    versionEntity.FilePath = Path.Combine(document.DocumentId.ToString(), document.FileName);

                    var version = await _docVersionService.CreateVersionAsync(versionEntity, 1);
                    version.Action = "create";

                    document.Version = version.VersionId;

                    
                    existingDocMap[key] = document;

                    remainingBytes -= physicalFile.Length;

                    summary.Success++;
                }
                catch (Exception ex)
                {
                    summary.Failed++;
                    summary.FailedDocDetails.Add($"Error: {record.FileName} - {ex.Message}");
                }
            }

            summary.TotalProcessed = metadataResult.TotalRecords;

            return summary;
        }


        //---------------------------------------------------------------------------//

        //---------chunk upload--------------------

        public async Task<DocumentResponseDto?> UploadDocumentChunks(DocumentUploadDto dto, int? currentuserid, string? username, string? fullname)
        {
            if (dto.TotalChunks == null || dto.TotalChunks <= 1)
            {
                // OLD behavior (small file)
                var tempTemp = _tempRoot.Replace("{StorageRoot}", _storageRoot).Replace("{ClientName}", _clientName);
                var tempDir = Path.Combine(tempTemp, Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                var tempFilePath = Path.Combine(
                    tempDir,
                    Path.GetFileName(dto.OriginalFileName ?? dto.File.FileName)
                );
                await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }
                try
                {
                    return await FinalizeUploadAsync(dto, currentuserid, tempFilePath, username, fullname);
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }

            // NEW behavior (chunk upload)
            return await UploadChunkAsync(dto, currentuserid, username, fullname);
        }

        private async Task<DocumentResponseDto?> UploadChunkAsync(DocumentUploadDto dto, int? currentuserid, string? username, string? fullname)
        {
            if (dto.TotalChunks <= 0)
                throw new BadRequestException("Invalid total chunks");

            if (dto.ChunkIndex < 0 || dto.ChunkIndex >= dto.TotalChunks)
                throw new BadRequestException("Invalid chunk index");

            if (dto.File.Length == 0)
                throw new BadRequestException("Empty chunk received");

            if (string.IsNullOrWhiteSpace(dto.UploadId))
                throw new BadRequestException("UploadId is required");
            var tempTemp= _tempRoot.Replace("{StorageRoot}", _storageRoot).Replace("{ClientName}", _clientName);
            var tempDir = Path.Combine(tempTemp, dto.UploadId);
            var chunksDir = Path.Combine(tempDir, "chunks");
            var mergedDir = Path.Combine(tempDir, "merged");

            Directory.CreateDirectory(chunksDir);
            Directory.CreateDirectory(mergedDir);

            var chunkPath = Path.Combine(chunksDir, $"{dto.ChunkIndex}.part");

            await using (var stream = new FileStream(
                    chunkPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: true))
            {
                await dto.File.CopyToAsync(stream);
            }


            var partCount = Directory.GetFiles(chunksDir, "*.part").Length;
            if (partCount < dto.TotalChunks)
            {
                // not all chunks yet
                return null;
            }


            string mergedFilePath;
            try
            {
                mergedFilePath = await MergeChunksAsync(chunksDir, mergedDir, dto.OriginalFileName, dto.TotalChunks);
            }
            catch
            {

                throw new ServerException("Failed to merge file chunks.");
            }

            DocumentResponseDto result;
            try
            {
                result = await FinalizeUploadAsync(dto, currentuserid, mergedFilePath, username, fullname);

            }
            catch (Exception ex)
            {

                throw new ServerException($"Failed to save document {ex.InnerException}");
            }

            // Cleanup ONLY after success
            try
            {
                Directory.Delete(tempDir, recursive: true);
                if (File.Exists(mergedFilePath))
                    File.Delete(mergedFilePath);
            }
            catch
            {
                throw new ServerException("Failed to clean up temporary files after upload.");
            }

            return result;

        }

        private async Task<DocumentResponseDto> FinalizeUploadAsync(DocumentUploadDto dto, int? currentuserid, string mergedFilePath, string? username, string? fullname)
        {
            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId)
                ?? throw new Exception("Invalid CabinetId");

            await _storageQuotaService.ValidateAndConsumeStorage(dto.File.Length);

            if (dto.DOB.HasValue && dto.DOJ.HasValue && dto.DOJ < dto.DOB)
            {
                throw new ValidationException("DOJ cannot be earlier than DOB");
            }
            if(dto.DOJ> DateTime.UtcNow || dto.DOB > DateTime.UtcNow)
            {
                throw new ValidationException("DOJ or DOB cannot be in the future");
            }
            if(dto.DOB.HasValue && (DateTime.UtcNow.Year - dto.DOB.Value.Year) < 18)
            {
                throw new ValidationException("DOB indicates age less than 18 years, please verify");
            }
            

            //validation for mandatory firlds
            var missingFields = CabinetDuplicateRulesHelper.ValidateMandatoryFields(dto.CabinetId, dto);
            if (missingFields.Any() || missingFields.Count != 0 )
            {
                throw new ValidationException(
                    $"Missing required fields for Cabinet {dto.CabinetId}: {string.Join(", ", missingFields)}"
                );
            }
            // duplicate check
            var duplicateDoc = await _repo.FindDuplicateAsync(dto, username, fullname);

            if (duplicateDoc != null && string.IsNullOrWhiteSpace(dto.Action))
            {
                return new DocumentResponseDto
                {
                    
                    DocumentId = duplicateDoc.DocumentId,
                    Version = duplicateDoc.Version,
                    Actions = new[] { "REPLACE", "MERGE" }
                };
            }

            int newVersion = duplicateDoc == null
                ? 1
                : await _repo.GetLatestVersion(duplicateDoc.DocumentId) + 1;

            DocumentTypes? docType = null;
            if (!string.IsNullOrWhiteSpace(dto.DocumentType))
                docType = await _uow.Documents.GetDocTypeDetailsByNameAsync(dto.DocumentType);

            var uploadRootTemplate= _uploadRoot .Replace("{StorageRoot}", _storageRoot).Replace("{ClientName}", _clientName);
            string CabName = cabinet.CabinetName;
            string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string finalFolder = Path.Combine(uploadRootTemplate, CabName, dateFolder);

            if (!Directory.Exists(finalFolder))
                Directory.CreateDirectory(finalFolder);

            var ext = Path.GetExtension(dto.OriginalFileName);
            var baseName = Path.GetFileNameWithoutExtension(dto.OriginalFileName);
            
            string fileName =
                $"{baseName}_v{newVersion}{ext}";
            string fullPath = Path.Combine(finalFolder, fileName);
            using var transaction = await _uow.BeginTransactionAsync();
            Document doc;
            try
            {    
                if (duplicateDoc == null)
                {
                    File.Move(mergedFilePath, fullPath, overwrite: true);

                    doc = await _repo.CreateDocument(new Document
                    {
                        CabinetId = dto.CabinetId,
                        FileName = fileName,
                        //FilePath = $@"\storage\Uploads\{dateFolder}\{folderName}\{fileName}",
                        FilePath = $@"{CabName}\{dateFolder}\{fileName}",
                        UploadedBy = currentuserid,
                        //Version = version,
                        Status = "active",
                        UploadedAt = DateTime.UtcNow,
                        DocumentTypeId = docType?.Id,
                        InvoiceNumber = dto.InvoiceNumber,

                        ManufactureId = dto.ManufactureId,
                        LoginId = username,
                        LoginName = fullname,
                        Period = DateFormatterHelper.ParsePeriod(dto.Period),
                        Remarks = dto.Remarks,

                        InvoiceDate = dto.InvoiceDate,
                        Amount = dto.Amount,
                 
                        StatementDate = dto.StatementDate,
                        PaidAmount = dto.PaidAmount,
                        Department = dto.Department,
                        Designation = dto.Designation,
                        Name = dto.Name,
                        EmployeeId = dto.EmployeeId,
                       
                        ContactNumber = dto.ContactNumber,
                        DOB = dto.DOB,
                        DOJ = dto.DOJ,
                        
                        Region=dto.Region//-----------------------only for Hr cabinet
                    });

                    await _storageQuotaService.ValidateAndConsumeStorage(dto.File.Length);
                    await _uow.CompleteAsync();

                }
                else
                {
                    doc = duplicateDoc;
                    var oldfile = Path.Combine(uploadRootTemplate, duplicateDoc.FilePath);

                    if (dto.Action?.ToUpper() == "REPLACE")
                    {
                        //if (File.Exists(oldfile))
                        //    File.Delete(oldfile);
                        File.Move(mergedFilePath, fullPath, overwrite: true);
                    }
                    else if (dto.Action?.ToUpper() == "MERGE")
                    {
                        await MergeUploadFileAsync(oldfile, dto.File, fullPath);
                    }
                    doc.FileName = fileName;
                    doc.FilePath = $@"{CabName}\{dateFolder}\{fileName}";
                    doc.UploadedAt = DateTime.UtcNow;

                    _uow.Documents.Update(doc);
                }
                // Create version history

                //var versionTemp = _versionRoot.Replace("{StorageRoot}", _storageRoot).Replace("{ClientName}", _clientName);
                //var versionFolder = Path.Combine(versionTemp, doc.DocumentId.ToString());
                //var versionFullPath = Path.Combine(versionFolder, doc.FileName);
                //if (!Directory.Exists(versionFolder))
                //    Directory.CreateDirectory(versionFolder);
                //File.Copy(fullPath, versionFullPath);

                var versionEntity = _mapper.Map<DocumentVersion>(doc);
                //versionEntity.FilePath = Path.Combine(doc.DocumentId.ToString(), doc.FileName);
                var version = await _docVersionService.CreateVersionAsync(versionEntity, newVersion);
                doc.Version = version.VersionId;
                version.Action = string.IsNullOrWhiteSpace(dto.Action)
                ? "create"
                : dto.Action.ToLower();

                await _uow.CompleteAsync();
                await transaction.CommitAsync(); 
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                throw new ServerException($"Failed to save document, {ex}");
            }
            // enforce max 5 versions
           await EnforceVersionLimit(doc.DocumentId);
           
            return _mapper.Map<DocumentResponseDto>(doc);
        }

        //---------------helper methods-----------------------------
        
        private async Task EnforceVersionLimit(int documentId)
        {
            await _docVersionRepo
                .GetOldVersionsToDelete(documentId, 5);
            var versions=await _docVersionRepo.GetArchivedVersions(documentId);
            if (versions.Count != 0)
            {
                var uploadRootTemplate = _uploadRoot.Replace("{StorageRoot}", _storageRoot).Replace("{ClientName}", _clientName);
                foreach (var v in versions)
                {
                    var fullPath = Path.Combine(uploadRootTemplate, v.FilePath);
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }

                await _docVersionRepo.DeleteOldArchivedVersions(documentId);
            }
        }

        private async Task<string> MergeChunksAsync(string chunksDir, string mergedDir, string originalFileName, int? totalChunks)
        {
            if (totalChunks == null || totalChunks <= 0)
                throw new ArgumentException("Invalid totalChunks", nameof(totalChunks));

            var safeFileName = Path.GetFileName(originalFileName);
            var mergedPath = Path.Combine(mergedDir, safeFileName);

            if (File.Exists(mergedPath))
                File.Delete(mergedPath);

            // Use a larger buffer to speed up I/O (16 MB)
            byte[] buffer = new byte[16 * 1024 * 1024];

            await using var finalStream = new FileStream(
                mergedPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                buffer.Length,
                useAsync: true
            );

            for (int i = 0; i < totalChunks; i++)
            {
                var partPath = Path.Combine(chunksDir, $"{i}.part");

                if (!File.Exists(partPath))
                    throw new BadRequestException($"Missing chunk {i}");

                await using var partStream = new FileStream(
                    partPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    buffer.Length,
                    useAsync: true
                );

                int bytesRead;
                while ((bytesRead = await partStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await finalStream.WriteAsync(buffer, 0, bytesRead);
                }
            }

            return mergedPath;
        }


        //--------------------- EXCEL PATCHING ----------------------

        public async Task<BatchResponseDTO> ApplyExcelPatchAsync(ExcelPatchRequestDto dto, int? userId)
        {
            var response = new BatchResponseDTO();
            var document = await _uow.Documents.GetDocument(dto.DocumentId);
            if (document == null || document.Status == "archived")
                throw new Exception("Document not found");

            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            if (cabinet == null || document.CabinetId != dto.CabinetId)
                throw new Exception("Invalid CabinetId");


            var excelPath = document.FilePath;

            using var fileStream = new FileStream(
                excelPath,
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

        //----------------------------  AUTO SUGGESTION -----------------------------------------------//

        public async Task<List<string>> GetSuggestionsAsync(AutoSuggestionRequestDto dto)
        {
            var suggestions = new List<string>();

            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_get_auto_suggestions(@p_field_name, @p_search_text, @p_limit)",
                conn);

            cmd.Parameters.AddWithValue("p_field_name", NpgsqlTypes.NpgsqlDbType.Varchar, dto.FieldName);
            cmd.Parameters.AddWithValue("p_search_text", NpgsqlTypes.NpgsqlDbType.Varchar, dto.SearchText);
            cmd.Parameters.AddWithValue("p_limit", NpgsqlTypes.NpgsqlDbType.Integer, dto.Limit);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                suggestions.Add(reader.GetString(0));
            }

            return suggestions;
        }

        //------------------    AUTO FIL    -------------------------------------

        public async Task<object> GetAutoFillAsync(AutoFillRequestDto dto)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM get_auto_fill(@p_cabinet_id,@p_field_name, @p_search_text, @p_limit)", conn);

            cmd.Parameters.AddWithValue("@p_cabinet_id", NpgsqlTypes.NpgsqlDbType.Integer, dto.Cabinet);
            cmd.Parameters.AddWithValue("@p_field_name", NpgsqlTypes.NpgsqlDbType.Varchar, dto.FieldName);
            cmd.Parameters.AddWithValue("@p_search_text", NpgsqlTypes.NpgsqlDbType.Varchar, dto.SearchText);
            cmd.Parameters.AddWithValue("@p_limit", NpgsqlTypes.NpgsqlDbType.Integer, dto.Limit);

            await using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<JsonElement>();

            while (await reader.ReadAsync())
            {
                var json = reader.GetString(0);
                results.Add(JsonSerializer.Deserialize<JsonElement>(json));
            }

            return results;
        }

        //-------------------------     SPLIT & EXTRACT PDF ----------------------

        public async Task<DocumentResponseDto> SplitRegularDocumentAsync(SplitAndExtractPdfDto dto, Cabinet cab, int? userId, string? username, string? fullname)
        {
            var originalDoc = await _repo.GetDocument(dto.Id)
                ?? throw new KeyNotFoundException("Document not found");

            //var cabinet = await _uow.Cabinets.GetByIdAsync(originalDoc.CabinetId)
            //    ?? throw new Exception("Invalid cabinet");


            // Resolve physical original file path
            var relativePath = originalDoc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var uploadRootTemplate = _uploadRoot.Replace("{StorageRoot}", _storageRoot).Replace("{ClientName}", _clientName);
            var fullPath = Path.Combine(uploadRootTemplate, relativePath);

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            string tempExtractedPath = Path.GetTempFileName();
            string tempRemainingPath = Path.GetTempFileName();

            try
            {
                using var originalPdf =
                    PdfReader.Open(fullPath, PdfDocumentOpenMode.Import);



                if (dto.FromPage < 1 || dto.ToPage < 1 || dto.FromPage > dto.ToPage || dto.ToPage > originalPdf.PageCount)
                    throw new InvalidOperationException($"Invalid page range. Document has {originalPdf.PageCount} pages.");


                //if (dto.ToPage > originalPdf.PageCount)
                //    throw new InvalidOperationException("Page range exceeds document length");

                var extractedPdf = new PdfDocument();
                var remainingPdf = new PdfDocument();

                for (int i = 0; i < originalPdf.PageCount; i++)
                {
                    int pageNumber = i + 1;

                    if (pageNumber >= dto.FromPage && pageNumber <= dto.ToPage)
                        extractedPdf.AddPage(originalPdf.Pages[i]);
                    else
                        remainingPdf.AddPage(originalPdf.Pages[i]);
                }

                if (extractedPdf.PageCount == 0)
                    throw new InvalidOperationException("No pages extracted");

                extractedPdf.Save(tempExtractedPath);
                remainingPdf.Save(tempRemainingPath);

                // ---------------- DB TRANSACTION ----------------
                using var transaction = await _uow.BeginTransactionAsync();

                // Resolve folder structure (same as UploadDocument)
                string folderName = cab.CabinetName;
                string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string cabinetFolder = Path.Combine(uploadRootTemplate, folderName, dateFolder);

                if (!Directory.Exists(cabinetFolder))
                    Directory.CreateDirectory(cabinetFolder);

                // Versioning &filenaming
                var filenaming = $"{dto.DocumentType}_{originalDoc.Name}";
                var orgext = Path.GetExtension(originalDoc.FileName);

                int newVersion = await _repo.GetLatestVersion(
                    originalDoc.DocumentId) + 1;

                string newFileName =
                    $"{filenaming}_v{newVersion}{orgext}";

                string newPhysicalPath = Path.Combine(cabinetFolder, newFileName);

                var docType =
                    await _uow.Documents.GetDocTypeDetailsByNameAsync(dto.DocumentType);

                var newDocument = new Document
                {
                    CabinetId = originalDoc.CabinetId,
                    FileName = newFileName,
                    //FilePath = $@"\storage\Uploads\{dateFolder}\{folderName}\{newFileName}",
                    FilePath = $@"{folderName}\{dateFolder}\{newFileName}",
                    UploadedBy = userId,
                    UploadedAt = DateTime.UtcNow,
                    // Version = version,
                    Status = "active",
                    DocumentTypeId = docType?.Id,


                    InvoiceNumber = originalDoc.InvoiceNumber,

                    InvoiceDate = originalDoc.InvoiceDate,
                    Amount = originalDoc.Amount,

                    StatementDate = originalDoc.StatementDate,
                    PaidAmount = originalDoc.PaidAmount,
                    Department = originalDoc.Department,
                    Designation = originalDoc.Designation,
                    Name = originalDoc.Name,
                    EmployeeId = originalDoc.EmployeeId,

                    ContactNumber = originalDoc.ContactNumber,
                    DOB = originalDoc.DOB,
                    DOJ = originalDoc.DOJ,

                    ManufactureId = originalDoc.ManufactureId,
                    Period = originalDoc.Period,
                    LoginName = fullname,
                    LoginId = username,
                    Remarks = originalDoc.Remarks
                };

                await _repo.CreateDocument(newDocument);
                await _uow.CompleteAsync();
                //  Move files only AFTER DB success
                File.Move(tempExtractedPath, newPhysicalPath, overwrite: true);
                File.Move(tempRemainingPath, fullPath, overwrite: true);


                var versionEntity = _mapper.Map<DocumentVersion>(newDocument);
                var version = await _docVersionService.CreateVersionAsync(versionEntity, newVersion);
                newDocument.Version = version.VersionId;
                version.Action = "split";



                await transaction.CommitAsync();

                return _mapper.Map<DocumentResponseDto>(newDocument);
            }
            catch
            {
                // Cleanup temp files
                if (File.Exists(tempExtractedPath))
                    File.Delete(tempExtractedPath);

                if (File.Exists(tempRemainingPath))
                    File.Delete(tempRemainingPath);

                throw;
            }
        }

        public async Task<DocumentResponseDto> SplitAndExtractPdfAsync(SplitAndExtractPdfDto dto,int? userId,string? username,string? fullname)
        {
            //var sourceDoc = await GetSourceDocument(dto);

            //if (sourceDoc == null)
            //    throw new NotFoundException("Document not found");

            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);

            if (cabinet == null)
                throw new ValidationException("Invalid cabinet");
            
            bool isHrOnboardingCabinet=false;
            if (dto.Source == DocumentSourceType.Onboarding && cabinet.CabinetId == 2)
                isHrOnboardingCabinet = true;
            

            if (isHrOnboardingCabinet)
            {
                return await _configService.SplitOnboardingDocumentAsync(
                    dto
                    //sourceDoc,
                    //userId,
                    //username,
                    //fullname
                    );
            }

            return await SplitRegularDocumentAsync(
                dto,
                cabinet,
                userId,
                username,
                fullname);
        }
        //------------------------- DOCUMENT DOWNLOAD LINK ----------------------------------//

        // get all document for download
        public async Task<List<DocDownloadGetDTO>> GetAllDocumentForDownloadAsync(int? userid)
        {
            var documents = await _uow.Documents.GetAllDocumentForDownload(userid);

            //if (documents == null || documents.Count == 0)
            //    throw new NotFoundException("No documents available for download.");
            return documents ?? new List<DocDownloadGetDTO>();
        }

        //downlaod encrypt

        public async Task<DocumentStreamResultDTO?> GenerateProtectedDownloadAsync(DocumentRequestDto dto, int? userid)
        {
            if(dto == null)
                throw new BadRequestException("Invalid request data.");


            if (dto.Source == DocumentSourceType.Document)
            {
                var doc = await _repo.GetDocument(dto.Id);

                if (doc == null)
                    throw new NotFoundException("Document not found");
            }
            else
            {
                var onboardingDoc = await _configrepo.GetOnboardingFilesAsync(dto.Id);

                if (onboardingDoc == null)
                    throw new NotFoundException("Document not found");
            }


            //var user = await _userrepo.GetByIdAsync(userid);
            ////if (user == null)
            ////    throw new NotFoundException($"User with id {userid} not found");

            //if (user == null)
            //    throw new NotFoundException($"User with id {userid} not found");


            //var relativePath = doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            //var originalFilePath = Path.Combine(_storageRoot, relativePath);

            //if (!File.Exists(originalFilePath))
            //    throw new NotFoundException("File not found in storage");

            //var password = GeneratePassword();

            //var tempDir = Path.Combine(Path.GetTempPath(), "ProtectedDownloads");
            //if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            //var ext = Path.GetExtension(originalFilePath).ToLower();
            //var tempFilePath = Path.Combine(tempDir, $"{Guid.NewGuid()}{ext}");

            //switch (ext)
            //{
            //    case ".pdf":
            //        await ProtectPdf(originalFilePath, tempFilePath, password);
            //        break;
            //    case ".xlsx":
            //        await ProtectExcel(originalFilePath, tempFilePath, password);
            //        break;
            //    default:
            //        throw new NotSupportedException("Unsupported file type");
            //}

            //var downloadLink = await _uow.Documents.GetByIdDownloadLinkAsync(docid, userid);

            //if (downloadLink.ExpiryDate <= DateTime.UtcNow)
            //    throw new UnauthorizedAccessException("Download link has expired.");

            //if (downloadLink.CurrentDownloads >= downloadLink.MaxDownloads)
            //    throw new UnauthorizedAccessException("Download limit exceeded.");


            //var data = await GetDocumentStream(docid);




            var rowsincremented = await _repo.CounterDocumentDownload(dto, userid);
            if (rowsincremented == 0)//not updated any rows
                throw new BadRequestException("Download limit exceeded or link expired.");
            var streamResult = await GetDocumentStream(dto);
            if (streamResult == null)
                throw new NotFoundException("Unable to generate document stream.");
            //if (data != null)
            //{
            //    downloadLink.CurrentDownloads++;
            //    await _uow.CompleteAsync();
            //}


            return streamResult;

        }


        //var htmlBody = $"""
        //    <html>
        //      <body style="font-family: Arial, sans-serif; line-height:1.5; color:#333;">
        //        <div style="max-width:600px; margin:auto; padding:20px; border:1px solid #e0e0e0; border-radius:8px;">
        //          <h2 style="color:#1a73e8;">Secure Document Access</h2>
        //          <p>Dear User,</p>
        //          <p>You have downloaded the document: <strong>{doc.FileName}</strong>.</p>
        //          <p>For security, this file is password-protected. Use the password below to open it:</p>
        //          <p style="font-size:16px; font-weight:bold; background:#f5f5f5; padding:10px; border-radius:4px; display:inline-block;">
        //            {password}
        //          </p>
        //          <p style="margin-top:20px;">
        //            Please keep this password confidential. It is required each time you open the file.
        //          </p>
        //          <p style="margin-top:10px;">
        //            If you did not initiate this download, contact your administrator immediately.
        //          </p>
        //          <hr style="border:none; border-top:1px solid #e0e0e0; margin:20px 0;" />
        //          <p style="font-size:12px; color:#888;">This is an automated message. Do not reply to this email.</p>
        //        </div>


        //      </body>
        //    </html>
        //    """;
        //await _emailSender.SendAsync(
        //            toEmail: user.Email, null, null,
        //            subject: "Password for Document download",
        //            htmlBody: htmlBody);

        //return new ProtectedFileResultdto
        //{
        //    ProtectedFilePath = tempFilePath
        //};
        //}

        //if (downloadLink == null || downloadLink.ExpiryDate < DateTime.UtcNow || downloadLink.CurrentDownloads == downloadLink.MaxDownloads)
        //    throw new NotFoundException("Document download time is expired/Exceeded the download limit");

        // This return is unreachable, but required to satisfy the compiler.
        // The method will always throw or return above.
        //throw new NotFoundException("Document download time is expired/Exceeded the download limit");
        // }

        //private static string GeneratePassword()
        //{
        //    return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        //        .Replace("+", "")
        //        .Replace("/", "")
        //        .Substring(0, 10);
        //}

        ////public static void ProtectPdf(string inputPath, string outputPath, string password)
        ////{
        ////    // Open existing PDF
        ////    var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);

        ////    // Set user password
        ////    document.SecuritySettings.UserPassword = password;

        ////    // Optionally, set owner password and permissions
        ////    document.SecuritySettings.OwnerPassword = password;
        ////    document.SecuritySettings.PermitPrint = true;
        ////    document.SecuritySettings.PermitModifyDocument = false;

        ////    document.Save(outputPath);
        ////}


        ////public static void ProtectExcel(string inputPath, string outputPath, string password)
        ////{
        ////    using var excelEngine = new ExcelEngine();
        ////    var application = excelEngine.Excel;
        ////    application.DefaultVersion = ExcelVersion.Excel2016;

        ////    // Open workbook
        ////    var workbook = application.Workbooks.Open(inputPath);

        ////    // Protect workbook (structure + windows)
        ////    workbook.Protect(true,true,password);

        ////    workbook.SaveAs(outputPath);
        ////}
        ///large file
        //public static async Task ProtectPdf(string inputPath, string outputPath, string password)
        //{
        //    // iText7 supports streaming encryption
        //    var writerProperties = new iTextPdf.WriterProperties()
        //        .SetStandardEncryption(
        //            System.Text.Encoding.UTF8.GetBytes(password), // user password
        //            System.Text.Encoding.UTF8.GetBytes(password), // owner password
        //            0, // permissions (0 = no special permissions)
        //            iTextPdf.EncryptionConstants.ENCRYPTION_AES_128 | iTextPdf.EncryptionConstants.DO_NOT_ENCRYPT_METADATA // encryptionAlgorithm
        //        );

        //    await using (var readerStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024 * 1024, useAsync: true))
        //    await using (var writerStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 16 * 1024 * 1024, useAsync: true))
        //    {
        //        using var pdfReader = new iTextPdf.PdfReader(readerStream);
        //        using var pdfWriter = new iTextPdf.PdfWriter(writerStream, writerProperties);

        //        using var pdfDoc = new iTextPdf.PdfDocument(pdfReader, pdfWriter);
        //        // No need to load entire document: encryption is applied on the fly
        //    }
        //}

        //public static async Task ProtectExcel(string inputPath, string outputPath, string password)
        //{
        //    // Copy the file first (like your chunk merge)
        //    File.Copy(inputPath, outputPath, overwrite: true);

        //    await Task.Run(() =>
        //    {
        //        using var spreadsheet = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(outputPath, true);

        //        var workbookPart = spreadsheet.WorkbookPart;
        //        var protection = new DocumentFormat.OpenXml.Spreadsheet.WorkbookProtection
        //        {
        //            LockStructure = true,
        //            LockWindows = true,
        //            WorkbookPassword = Convert.ToBase64String(System.Security.Cryptography.SHA1.HashData(
        //                System.Text.Encoding.UTF8.GetBytes(password)
        //            ))
        //        };

        //        workbookPart.Workbook.AppendChild(protection);
        //        workbookPart.Workbook.Save();
        //    });
        //}

        //------------excel view-------------------------
        //get sheet names
        public async Task<List<ListDto>> GetExcelSheetNamesAsync(int documentId)
        {
            var document = await _repo.GetDocument(documentId);
            if (document == null)
                throw new KeyNotFoundException("Document not found");

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

            return sheets;
        }


        public async Task<string> OpenExcelSheetAsync(DocumentExcelOpenDTO dto)
        {
            var document = await _repo.GetDocument(dto.DocumentId);
            if (document == null)
                throw new KeyNotFoundException("Document not found");

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

        //helper method

        public async Task MergeUploadFileAsync(string existingFilePath, IFormFile newFile, string outputPath)
        {
            if (!File.Exists(existingFilePath))
                throw new FileNotFoundException("Existing file not found", existingFilePath);

            var tempFile = Path.GetTempFileName();

            try
            {
                // save incoming file to temp
                using (var stream = new FileStream(tempFile, FileMode.Create))
                {
                    await newFile.CopyToAsync(stream);
                }

                using var outputDocument = new PdfDocument();

                // existing PDF
                using (var existingDocument = PdfReader.Open(existingFilePath, PdfDocumentOpenMode.Import))
                {
                    for (int i = 0; i < existingDocument.PageCount; i++)
                    {
                        outputDocument.AddPage(existingDocument.Pages[i]);
                    }
                }

                // new PDF
                using (var newDocument = PdfReader.Open(tempFile, PdfDocumentOpenMode.Import))
                {
                    for (int i = 0; i < newDocument.PageCount; i++)
                    {
                        outputDocument.AddPage(newDocument.Pages[i]);
                    }
                }

                outputDocument.Save(outputPath);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                //if (File.Exists(existingFilePath))
                //    File.Delete(existingFilePath);
            }
        }


        public async Task<List<ManfactureDto>> GetManufactureDetailsAsync()
        {
            return await _repo.GetManufactureDetailsList();
          
        }
    }

}
