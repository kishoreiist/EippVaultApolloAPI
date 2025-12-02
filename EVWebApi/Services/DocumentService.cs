using AutoMapper;
using EVWebApi.DTOs;
using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        private readonly IWebHostEnvironment _env;
        private readonly IMetadataRepository _metadataRepo;

        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;


        public DocumentService(IDocumentRepository repo, IMetadataRepository metadataRepo, IWebHostEnvironment env, IUnitOfWork uow, IMapper mapper)
        {
            _repo = repo;
            _metadataRepo = metadataRepo;
            _env = env;
            _uow = uow;
            _mapper = mapper;
        }

        // ---------------------- UPLOAD ----------------------
        public async Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto,int currentuserid)
        {
            if (dto.File == null)
                throw new BadRequestException("File is required");

            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            if (cabinet == null)
                throw new Exception("Invalid CabinetId");
            string folderName = cabinet.CabinetName;
            string storageRoot = Path.Combine(_env.WebRootPath, "storage");
            string cabinetFolder = Path.Combine(storageRoot, folderName);
            if (!Directory.Exists(cabinetFolder))
                Directory.CreateDirectory(cabinetFolder);

            // Versioning logic
            int version = await _repo.GetLatestVersion(dto.CabinetId, dto.File.FileName) + 1;

            // Create unique filename
            string fileName = $"{Path.GetFileNameWithoutExtension(dto.File.FileName)}_v{version}{Path.GetExtension(dto.File.FileName)}";
            string fullPath = Path.Combine(cabinetFolder, fileName);

            // Save file physically
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }


                // Save to DB
                var doc = await _repo.CreateDocument(new Document
            {
                CabinetId = dto.CabinetId,
                FileName = fileName,
                FilePath = $"/storage/{folderName}/{fileName}",
                UploadedBy = currentuserid,
                Version = version,
                Status = "active",
                UploadedAt= DateTime.UtcNow
            });

            if (doc == null)
                throw new ServerException("Failed to save document");

            if (dto.Metadata != null && dto.Metadata.Any())
            {
                var metadataList = dto.Metadata.Select(x => new Metadata
                {
                    DocumentId = doc.DocumentId,
                    MetaKey = x.Key,
                    MetaValue = x.Value
                }).ToList();

                await _metadataRepo.AddMetadata(metadataList);
            }


            return new DocumentResponseDto
            {
                DocumentId = doc.DocumentId,
                CabinetId=doc.CabinetId,
                FileName = doc.FileName,
                FilePath=doc.FilePath,
                UploadedAt = doc.UploadedAt,
                Status = doc.Status,
                Version = doc.Version,
                Metadata = dto.Metadata?.Select(x => new MetadataDTO
                {
                    Key = x.Key,
                    Value = x.Value
                }).ToList()
            };

        }


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
        public async Task<PagedResponse<DocumentResponseDto>> GetDocumentsByCabinetId(int cabinetId,DocumentQueryParameters query)
        {
            var docQuery = _uow.Documents.Query()
                .Where(d => d.CabinetId == cabinetId);
            //d.Status == "active");

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
                    docQuery = docQuery.Where(d => d.InvoiceNumber.StartsWith(query.InvoiceNumber));
                else
                    docQuery = docQuery.Where(d => d.InvoiceNumber.Contains(query.InvoiceNumber));
            }
            //VendorNumber
            if (!string.IsNullOrWhiteSpace(query.VendorNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.VendorNumber.StartsWith(query.VendorNumber));
                else
                    docQuery = docQuery.Where(d => d.VendorNumber.Contains(query.VendorNumber));
            }
            //Check number
            if (!string.IsNullOrWhiteSpace(query.CheckNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.CheckNumber.StartsWith(query.CheckNumber));
                else
                    docQuery = docQuery.Where(d => d.CheckNumber.Contains(query.CheckNumber));
            }
            //GST
            if (query.GST.HasValue)
            {
                var gstStr = query.GST.Value.ToString();

                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                {
                    docQuery = docQuery.Where(d => d.GST.ToString().StartsWith(gstStr));
                }
                else
                {
                    docQuery = docQuery.Where(d => d.GST.ToString().Contains(gstStr));
                }
            }
            //PO NUMBER
            if (!string.IsNullOrWhiteSpace(query.PoNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.PoNumber.StartsWith(query.PoNumber));
                else
                    docQuery = docQuery.Where(d => d.PoNumber.Contains(query.PoNumber));
            }
            //EMP ID
            if (!string.IsNullOrWhiteSpace(query.EmployeeId))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.EmployeeId.StartsWith(query.EmployeeId));
                else
                    docQuery = docQuery.Where(d => d.EmployeeId.Contains(query.EmployeeId));
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


            // TOTAL BEFORE PAGINATION
            var totalRecords = docQuery.Count();
            docQuery = docQuery
                .Include(d => d.MetadataList);
            // APPLY PAGINATION
            var pagedDocs = docQuery
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToList();

            // MAP TO DTO

            
            var docDtos = _mapper.Map<List<DocumentResponseDto>>(pagedDocs);

            return new PagedResponse<DocumentResponseDto>
            {
                Data = docDtos,
                TotalRecords = totalRecords,
                Offset = query.Offset,
                Limit = query.Limit
            };
        }

        // ---------------------- PREVIEW STREAM ----------------------
        public async Task<Stream?> GetDocumentStream(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null)
                throw new NotFoundException("Document not found");

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, doc.FilePath.TrimStart('/').Replace("/", "\\"));

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        }

        // ---------------------- DOWNLOAD ----------------------
        public async Task<DocumentDownloadDto?> GetDocumentForDownload(int id)
        {
            var doc = await _repo.GetDocument(id);
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, doc.FilePath.TrimStart('/').Replace("/", "\\"));

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new DocumentDownloadDto { Stream = fs, FileName = doc.FileName };
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
        // ------------------DELETE--------------------
        public async Task<bool> DeleteDocument(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null)
                throw new NotFoundException("Document not found");

            // Delete metadata
            //await _metadataRepo.DeleteMetadataByDocumentId(id);
            //string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace("/", "\\"));
            //// Delete physical file
            //if (File.Exists(fullPath))
            //    File.Delete(fullPath);

            await _repo.DeleteDocument(id);

            return true;
        }

        //--------------------- EDIT ---------------------------------

        public async Task<DocumentResponseDto> UpdateDocumentAsync(int id, UpdateDocumentDto dto)
        {
            var document = await _uow.Documents.GetByIdAsync(id);

            if (document == null || document.Status == "inactive")
                throw new NotFoundException($"Document with id {id} not found");


            if (dto.CabinetId != 0)
                document.CabinetId = dto.CabinetId;

            if (!string.IsNullOrWhiteSpace(dto.FileName))
                document.FileName = dto.FileName;

            if (!string.IsNullOrWhiteSpace(dto.FilePath))
                document.FilePath = dto.FilePath;

            // Metadata update (if applicable)---------------------------need to check with sir
            //if (dto.Metadata != null && dto.Metadata.Any())
            //{
            //    // Your own logic here
            //    // Example: Replace metadata entries
            //    document.Metadata = dto.Metadata.Select(m => new Metadata
            //    {
            //        Key = m.Key,
            //        Value = m.Value
            //    }).ToList();
            //}

            if (dto.InvoiceNumber != null) document.InvoiceNumber = dto.InvoiceNumber;
            if (dto.PoNumber != null) document.PoNumber = dto.PoNumber;
            if (dto.VendorNumber != null) document.VendorNumber = dto.VendorNumber;
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
            if (dto.GST.HasValue) document.GST = dto.GST.Value;
            if (dto.CheckNumber != null) document.CheckNumber = dto.CheckNumber;
            if (dto.PaidAmount.HasValue) document.PaidAmount = dto.PaidAmount.Value;

            _uow.Documents.Update(document);
            await _uow.CompleteAsync();
            return _mapper.Map<DocumentResponseDto>(document);
        }


    }
}
