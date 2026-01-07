using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using EVWebApi.DTOs;
using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using EVWebApi.Models;
using EVWebApi.Repositories;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using static PdfSharpCore.Pdf.PdfDictionary;
using Document = EVWebApi.Models.Document;
namespace EVWebApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        private readonly IMetadataRepository _metadataRepo;
        private readonly IMetadataReaderFactoryService _metadataReaderFactory;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadRoot;
        private readonly string _storageRoot;

        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;


        public DocumentService(IDocumentRepository repo, IMetadataRepository metadataRepo, IMetadataReaderFactoryService metadataReaderFactory,
            IWebHostEnvironment env, IUnitOfWork uow, IMapper mapper, IConfiguration config)
        {
            _repo = repo;
            _metadataRepo = metadataRepo;
            _metadataReaderFactory = metadataReaderFactory;
            _env = env;
            _uow = uow;
            _mapper = mapper;
            _uploadRoot = config["UploadSettings:RootPath"];
            _storageRoot = config["DocumentSettings:StorageRoot"];
        }

        // ---------------------- SINGLE UPLOAD ----------------------
        public async Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto,int currentuserid)
        {
            if (dto.File == null)
                throw new BadRequestException("File is required");

            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            if (cabinet == null)
                throw new Exception("Invalid CabinetId");

            DocumentTypes? docType = null;
            if (!string.IsNullOrWhiteSpace(dto.DocumentType))
            {
                docType = await _uow.Documents.GetOrCreateDocLabelAsync(dto.DocumentType);
            }

            // string storageRoot =Path.Combine(_env.WebRootPath, "storage/Uploads");
            string folderName = cabinet.CabinetName;
            string basePath = _uploadRoot;
            string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string datePath = Path.Combine(basePath, dateFolder);
            string cabinetFolder = Path.Combine(datePath, folderName);



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
                FilePath = $@"\storage\Uploads\{dateFolder}\{folderName}\{fileName}",
                UploadedBy = currentuserid,
                Version = version,
                Status = "active",
                UploadedAt= DateTime.UtcNow,
                DocumentTypeId = docType?.Id,
                DocumentType = null,
                InvoiceNumber =dto.InvoiceNumber,
                VendorNumber=dto.VendorNumber,
                InvoiceDate=dto.InvoiceDate,
                Amount=dto.Amount,
                GST=dto.GST,
                StatementDate =dto.StatementDate,
                PaidAmount=dto.PaidAmount,
                Department=dto.Department,
                Designation=dto.Designation,
                Name=dto.Name,
                EmployeeId=dto.EmployeeId,
                PoNumber=dto.PoNumber,
                ContactNumber=dto.ContactNumber,
                DOB=dto.DOB,
                DOJ=dto.DOJ,
                CheckNumber=dto.CheckNumber
                });

            if (doc == null)
                throw new ServerException("Failed to save document");
            return _mapper.Map<DocumentResponseDto>(doc);
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

            //Document Type
            if (!string.IsNullOrWhiteSpace(query.DocType))
            {
                var docType = await _uow.Documents.GetOrCreateDocLabelAsync(query.DocType);
                docQuery = docQuery.Where(d => d.DocumentTypeId == docType.Id);

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
            //VendorNumber
            if (!string.IsNullOrWhiteSpace(query.VendorNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.VendorNumber.ToLower().StartsWith(query.VendorNumber.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.VendorNumber.ToLower().Contains(query.VendorNumber.ToLower()));
            }
            //Check number
            if (!string.IsNullOrWhiteSpace(query.CheckNumber))
            {
                if (query.SearchType == null || query.SearchType == SearchType.starts_with)
                    docQuery = docQuery.Where(d => d.CheckNumber.ToLower().StartsWith(query.CheckNumber.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.CheckNumber.ToLower().Contains(query.CheckNumber.ToLower()));
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
                    docQuery = docQuery.Where(d => d.PoNumber.ToLower().StartsWith(query.PoNumber.ToLower()));
                else
                    docQuery = docQuery.Where(d => d.PoNumber.ToLower().Contains(query.PoNumber.ToLower()));
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


            // TOTAL BEFORE PAGINATION
            //var totalRecords = docQuery.Count();
            //docQuery = docQuery
            //    .Include(d => d.MetadataList);
            //// APPLY PAGINATION
            //var pagedDocs = docQuery
            //    .Skip((query.PageNumber - 1) * query.PageSize)
            //    .Take(query.PageSize)
            //    .ToList();

            
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

        // ---------------------- PREVIEW STREAM ----------------------
        public async Task<Stream?> GetDocumentStream(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null)
                throw new NotFoundException("Document not found");

            var relativePath = doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(_storageRoot, relativePath);

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found in storage");

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024, useAsync: true);
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

        //--------------------------FILE EXPLORER-------------------

        public async Task<List<DocumentFileExplorer>> GetFileExplorerDocumentAsync(int id)
        {
            var cabinet = await _uow.Cabinets.GetByIdAsync(id);

            if (cabinet == null)
                throw new NotFoundException("Cabinet not found");

            var files = await _repo.GetFileExplorerAsync(id);
            if(files==null)
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
        public async Task<(int cabinetId, bool status)> DeleteDocument(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null)
                throw new NotFoundException("Document not found");
            int cabinetid = doc.CabinetId;

            await _repo.DeleteDocument(id);
            return (cabinetid, true);
        }

        // -----------------MULTI-DELETE--------------------
        public async Task<BatchResponseDTO> DeleteMultipleDocuments(List<int> ids)
        {
            var summary = new BatchResponseDTO();
            foreach (var id in ids)
            {
                summary.TotalProcessed++;
                try
                {
                    var doc = await _repo.GetDocument(id);
                    if (doc.Status == "archived")
                        throw new AuthorizationException("Access to archived document is forbidden");
                    if (doc != null)
                    {

                        await _repo.DeleteDocument(id);
                        summary.Success++;
                    }
                }
                catch (Exception ex)
                {
                    summary.FailedDocDetails.Add($"Error deleting Document ID {id}: {ex.Message}");
                    summary.Failed++;
                }
                
            }
            return summary;
        }
        //-------------------- Export Merged PDF-----------------------

        public async Task<Stream?> GetMergedDocumentStream(List<int> documentIds)
        {
            var outputStream = new MemoryStream();

            using (var outputDocument = new PdfSharpCore.Pdf.PdfDocument())
            {
                foreach (var id in documentIds)
                {
                    var pdfStream = await GetDocumentStream(id);
                    if (pdfStream == null)
                        throw new NotFoundException($"Document not found: {id}");

                    using (pdfStream)
                    using (var inputDocument = PdfSharpCore.Pdf.IO.PdfReader.Open(
                        pdfStream,
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

        public async Task<(Stream ZipStream, string ZipFileName)> GetZIPFile(BatchDocDto dto)
        {
            var memoryStream = new MemoryStream();

                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var id in dto.DocumentIds)
                    {
                        // Fetch PDF stream for invoice
                        var pdfStream = await GetDocumentStream(id);
                        if (pdfStream == null)
                            throw new NotFoundException($"Document not found: {id}");

                    // Generate file name
                    var originalFileName = await _repo.GetDocumentName(id); 

                    // Generate unique filename for ZIP: originalname_id.pdf
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                    var extension = Path.GetExtension(originalFileName);
                    var FileName = $"{fileNameWithoutExt}_{id}{extension}";

                    var zipEntry = archive.CreateEntry(FileName, CompressionLevel.Fastest);
                        using (var entryStream = zipEntry.Open())
                        {
                            await pdfStream.CopyToAsync(entryStream);
                        }
                    }
                }

            memoryStream.Position = 0;
            var cabinetName = await _uow.Cabinets.GetCabinetNameAsync(dto.CabinetId) ?? "UnknownCabinet";
            var safeCabinetName = string.Concat(cabinetName.Split(Path.GetInvalidFileNameChars()));
            var zipFileName = $"{safeCabinetName}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.zip";

            return (memoryStream, zipFileName);
            
        }

        //--------------------- EDIT ---------------------------------

        public async Task<DocumentResponseDto> UpdateDocumentAsync(int id, UpdateDocumentDto dto)
        {
            var document = await _uow.Documents.GetDocument(id);

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

            DocumentTypes? docType = null;
            if (!string.IsNullOrWhiteSpace(dto.DocumentType))
            {
                docType = await _uow.Documents.GetOrCreateDocLabelAsync(dto.DocumentType);
                document.DocumentTypeId = docType.Id;
                //document.DocumentType = null;
            }

            _uow.Documents.Update(document);
            await _uow.CompleteAsync();
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
                CreatedAt= DateTime.UtcNow,
                CreatedBy= CurrentUsername,
                DocumentId=dto.DocumentId
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

        public async Task<List<string>> GetDocTypeAsync()
        {
            var doctype = await _uow.Documents.GetDocTypesAsync();
            if (doctype == null)
                throw new NotFoundException("Document types  not found");
            return doctype;
        }


        // ----------------- BATCH UPLOAD------------------------

        public async Task<BatchResponseDTO> BatchUploadDocuments(BatchUploadDTO dto, int currentuserid)
        {
            if (dto.MetadataFile == null || dto.Files == null || dto.Files.Count == 0)
                throw new ArgumentException("CSV or PDF files are missing.");


            var cabinet = await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            if (cabinet == null)
                throw new Exception("Invalid CabinetId");

            var extension = Path.GetExtension(dto.MetadataFile.FileName).ToLower();
            var reader = _metadataReaderFactory.GetReader(extension);

            var metadataResult = await reader.ReadAsync(dto.MetadataFile);
            if (metadataResult.TotalRecords == 0 || metadataResult.Records.Count==0)
                throw new ArgumentException("Unable to read the metadata file");

            var summary = new BatchResponseDTO();
            summary.Failed += metadataResult.Errors.Count;
            summary.FailedDocDetails.AddRange(metadataResult.Errors);

            var processedFiles = new List<string>();
            int totalCount = 0;

            foreach (var record in metadataResult.Records)
            {
                totalCount++;

                try
                {
                    // 2. Find the physical file in the uploaded list matching the 'FileName' column in CSV
                    var physicalFile = dto.Files.FirstOrDefault(f =>
                        f.FileName.Equals(record.FileName, StringComparison.OrdinalIgnoreCase));

                    if (physicalFile == null)
                    {
                        summary.FailedDocDetails.Add($"Row {totalCount}:File '{record.FileName}' not found in the uploaded batch.");
                        summary.Failed++;
                        continue;
                    }

                    // 3. Save File to Storage

                    string folderName = cabinet.CabinetName;
                    string basePath = _uploadRoot;
                    string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    string datePath = Path.Combine(basePath, dateFolder);
                    string cabinetFolder = Path.Combine(datePath, folderName);

                    if (!Directory.Exists(cabinetFolder))
                        Directory.CreateDirectory(cabinetFolder);

                    // Versioning logic
                    int version = await _repo.GetLatestVersion(dto.CabinetId, record.FileName) + 1;

                    // Create unique filename
                    string fileName = $"{Path.GetFileNameWithoutExtension(record.FileName)}_v{version}{Path.GetExtension(record.FileName)}";
                    string fullPath = Path.Combine(cabinetFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await physicalFile.CopyToAsync(stream);
                        
                    }
                    processedFiles.Add(fullPath);
                    DocumentTypes? docType = null;
                    if (!string.IsNullOrWhiteSpace(record.DocumentType))
                    {
                         docType =await _uow.Documents.GetOrCreateDocLabelAsync(record.DocumentType);

                    }

                    // 4. Map to Database Entity
                    var document = new Document
                    {
                        CabinetId = dto.CabinetId,
                        //FileName = physicalFile.FileName,
                        FileName = fileName,
                        FilePath = $@"\storage\Uploads\{dateFolder}\{folderName}\{fileName}",
                        Version=version,
                        Status = "active",
                        // Metadata fields from CSV
                        InvoiceNumber = record.InvoiceNumber,
                        PoNumber = record.PoNumber,
                        VendorNumber = record.VendorNumber,
                        EmployeeId = record.EmployeeId,
                        Name = record.Name,
                        ContactNumber = record.ContactNumber,
                        Designation = record.Designation,
                        Department = record.Department,
                        InvoiceDate = record.InvoiceDate,
                        StatementDate = record.StatementDate,
                        DOJ = record.DOJ,
                        DOB = record.DOB,
                        Amount = record.Amount,
                        GST = record.GST,
                        CheckNumber = record.CheckNumber,
                        PaidAmount = record.PaidAmount,
                        DocumentTypeId = docType?.Id,
                        DocumentType = null,
                        Region = record.Region,
                        UploadedBy = currentuserid,
                        UploadedAt = DateTime.UtcNow
                    };

                    _repo.AddDocumentRange(document);
                    summary.Success++;
                }
                catch (Exception ex)
                {
                    summary.FailedDocDetails.Add($"Error processing {record.FileName}: {ex.Message}");
                    summary.Failed++;
                }
            }
            summary.TotalProcessed = metadataResult.TotalRecords;

            if (summary.Success > 0)
            {
                try
                {
                    await _uow.CompleteAsync();
                }
                catch (Exception ex)
                {
                    foreach (var path in processedFiles)
                    {
                        if (File.Exists(path)) File.Delete(path);
                    }

                    throw new Exception($"Database commit failed. All uploaded files have been rolled back. Error: {ex.Message}");
                }
            }

            return summary ;
        }
    }
    
}
