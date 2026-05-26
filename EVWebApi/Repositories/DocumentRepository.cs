using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using EVWebApi.Models.HR;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EVWebApi.Repositories
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        private new readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        // ---------------- CREATE DOCUMENT -------------------
        public async Task<Document> CreateDocument(Document doc)
        {
            _context.Documents.Add(doc);
            //await _context.SaveChangesAsync();
            return doc;
        }


        public void AddDocumentRange(Document doc)
        {
            _context.Documents.Add(doc);
        }

        // ---------------- GET DOCUMENT BY Doc ID----------------------
        public async Task<Document> GetDocument(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.MetadataList)
                .Include(d => d.Notes)
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null)
                throw new Exception("Document not found");


            return doc;
        }
        //--------------GET notes with docs while fetching  BY Cabinet ID------------------
        public IQueryable<Document> Query()
        {
            return _context.Documents
                .Include(d => d.Notes)
                 .Include(d => d.DocumentType).OrderByDescending(x => x.UploadedAt)
                .AsQueryable();
        }

        // ---------------- GET LATEST VERSION -----------------
        public async Task<int> GetLatestVersion(int docId)
        {
            var versionNo = await _context.DocumentVersion
                .Where(d => d.DocumentId == docId)
                .OrderByDescending(d => d.VersionId)
                .Select(d => d.VersionNo)
                .FirstOrDefaultAsync();
            return versionNo;
        }

        // ---------------- UPDATE STATUS -------------------
        public async Task UpdateStatus(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                throw new Exception("Document not found");

            doc.Status = "inactive";
            await _context.SaveChangesAsync();
        }
        // ---------------- DELETE -------------------
        public async Task<Document> DeleteDocument(int documentId)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc == null || doc.Status == "archived")
                throw new Exception("Document not found");
            else
            {
                //_context.Documents.Remove(doc);
                doc.Status = "archived";
                _context.Documents.Update(doc);
                await _context.SaveChangesAsync();
            }
            return doc;
        }

        //---------------------File explorer -----------------------
        //public async Task<List<DocumentFileExplorer>> GetFileExplorerAsync(int cabinetId)
        //{
        //    var files = await _context.Documents
        //.Where(d => d.CabinetId == cabinetId && d.Status == "active")
        //.Select(d => new DocumentFileExplorer
        //{

        //    DocumentId = d.DocumentId,
        //    FileName = d.FileName,
        //    FilePath = d.FilePath
        //})
        //.ToListAsync();

        //    return files;
        //}

        public async Task<List<DocumentFileExplorer>> GetFileExplorerAsync(int cabinetId)
        {
            var documents = await _context.Documents
                .Where(d => d.CabinetId == cabinetId && d.Status == "active")
                .Include(d => d.Candidate)
                    .ThenInclude(c => c.OnboardingDocs).AsNoTracking()
                 .OrderByDescending(d=>d.DocumentId)
                .ToListAsync();

            var result = new List<DocumentFileExplorer>();

            foreach (var d in documents)
            {
                // Candidate onboarding docs
                if (d.CandidateId != null && d.Candidate != null)
                {
                    result.AddRange(
                        d.Candidate.OnboardingDocs.Select(o => new DocumentFileExplorer
                        {
                            DocumentId = d.DocumentId,
                            OnboardingDocId=o.Id,
                            FileName = o.FileName,
                            FilePath = o.FilePath
                        }));
                }

                // Normal uploaded docs
                else if (!string.IsNullOrWhiteSpace(d.FileName) &&
                         !string.IsNullOrWhiteSpace(d.FilePath))
                {
                    result.Add(new DocumentFileExplorer
                    {
                        DocumentId = d.DocumentId,
                        FileName = d.FileName,
                        FilePath = d.FilePath
                    });
                }
            }

            return result;
        }
        //-----------GET NOTES BY DOC ID FROM DB------------------
        public override async Task<Document?> GetByIdAsync(int id)
        {
            return await Query()
                .Include(d => d.Notes)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
        }
        //------------  GET DOC TYPES-------------

        public async Task<List<DocTypeCreateDto>> GetDocTypesAsync()
        {
            var doctype = await _context.DocumentTypes
                .Select(d => new DocTypeCreateDto
                {
                    Label = d.Label,
                    Type = d.Type
                })
                .Distinct()
                .ToListAsync();
            if (doctype == null)
                throw new Exception("Document type not found");

            return doctype;
        }

        //---------------GET DOC TYPE DETAILS BY doc_type name---------------

        public async Task<DocumentTypes> GetOrCreateDocLabelAsync(DocTypeCreateDto dto)//can be used for filtering as it is dropdown
        {
            var label = dto.Label.Trim();
            var type = dto.Type.Trim();

            var existing = await _context.DocumentTypes
                .FirstOrDefaultAsync(x => x.Label == label);

            if (existing != null)
                return existing;

            var docType = new DocumentTypes
            {
                Key = GenerateDocKeyHelper.GenerateDocKey(label),
                Label = label,
                Status = true,
                Type=type
            };

            _context.DocumentTypes.Add(docType);
            await _context.SaveChangesAsync();
            return docType;
        }

        public async Task<DocumentTypes> GetDocTypeDetailsByNameAsync(string name)
        {
            var docType = await _context.DocumentTypes
                .FirstOrDefaultAsync(d => d.Label == name);
            if (docType == null)
                throw new NotFoundException($"Document type not found: {name}");
            return docType;
        }

        //------------------GET DOC NAME BY DocId--------------

        public async Task<string> GetDocumentName(int id)
        {
            var document = await _context.Documents
                .Where(d => d.DocumentId == id)
                .Select(d => new { d.FileName })
                .FirstOrDefaultAsync();

            if (document == null)
                throw new NotFoundException($"Document not found: {id}");

            return document.FileName;
        }
        //-----------------get doc by ids------------------//
        public async Task<List<Document>> GetDocumentsByIds(List<int> ids)
        {
            return await _context.Documents
                .Where(d => ids.Contains(d.DocumentId))
                .Include(d => d.DocumentType)
                .AsNoTracking()
                .ToListAsync();
        }
        //-------------------GET DOCS DETAILS BY ID FOR EXCEL EXPORT-------------//

        public async Task<List<Document>> ExcelExportQuery(ExportExcelDocDto dto)
        {
            var query = _context.Documents
                .Where(d => d.Status == "active")
                .Include(d => d.DocumentType)
                .AsNoTracking();

            if (dto.DocumentIds != null && dto.DocumentIds.Any())
            {
                query = query.Where(d => d.CabinetId == dto.CabinetId && dto.DocumentIds.Contains(d.DocumentId));
            }
            else
            {
                query = query
                    .Where(d => d.CabinetId == dto.CabinetId)
                    .OrderByDescending(d => d.UploadedAt)
                    .Take(500);
            }

            return await query.ToListAsync();
        }

        //public async Task<List<DocumentExportDto>> ExcelExportQuery(ExportExcelDocDto dto)
        //{
        //    var baseQuery = _context.Documents
        //        .AsNoTracking()
        //        .Where(d =>
        //            d.Status == "active" &&
        //            d.CabinetId == dto.CabinetId);

        //    // Filter selected documents
        //    if (dto.DocumentIds != null && dto.DocumentIds.Any())
        //    {
        //        baseQuery = baseQuery
        //            .Where(d => dto.DocumentIds.Contains(d.DocumentId));
        //    }
        //    else
        //    {
        //        baseQuery = baseQuery
        //            .OrderByDescending(d => d.UploadedAt)
        //            .Take(500);
        //    }

        //    // Fetch only required fields
        //    var documents = await baseQuery
        //        .Select(d => new
        //        {
        //            d.DocumentId,
        //            d.CandidateId,
        //            d.EmployeeId,
        //            d.Name,
        //            d.DOB,
        //            d.DOJ,
        //            d.ContactNumber,
        //            d.Designation,
        //            d.FileName,
        //            d.FilePath,
        //            DocumentType = d.DocumentType != null
        //                ? d.DocumentType.Label
        //                : null
        //        })
        //        .ToListAsync();

        //    // -----------------------------
        //    // NORMAL DOCUMENTS
        //    // -----------------------------
        //    var normalDocs = documents
        //        .Where(d =>
        //            !string.IsNullOrWhiteSpace(d.FileName) &&
        //            !string.IsNullOrWhiteSpace(d.FilePath))
        //        .Select(d => new DocumentExportDto
        //        {
        //            DocumentId = d.DocumentId,
        //            CandidateId = d.CandidateId,
        //            EmployeeId = d.EmployeeId,
        //            Name = d.Name,
        //            DOJ = d.DOJ,
        //            DOB = d.DOB,
        //            ContactNumber = d.ContactNumber,
        //            Designation = d.Designation,
        //            DocumentType = d.DocumentType,
        //            FileName = d.FileName,
        //            FilePath = d.FilePath
        //        })
        //        .ToList();

        //    // -----------------------------
        //    // ONBOARDING DOCUMENTS
        //    // -----------------------------

        //    // Distinct candidate ids only
        //    var candidateMap = documents
        //        .Where(d =>
        //            string.IsNullOrWhiteSpace(d.FileName) &&
        //            string.IsNullOrWhiteSpace(d.FilePath) &&
        //            d.CandidateId.HasValue)
        //        .GroupBy(d => d.CandidateId!.Value)
        //        .ToDictionary(g => g.Key, g => g.First());

        //    var candidateIds = candidateMap.Keys.ToList();

        //    var onboardingDocs = new List<DocumentExportDto>();

        //    if (candidateIds.Any())
        //    {
        //        onboardingDocs = await _context.OnboardingHRDocument
        //            .AsNoTracking()
        //            .Where(x =>
        //                candidateIds.Contains(x.CandidateId) &&
        //                x.Status == "active")
        //            .Select(x => new DocumentExportDto
        //            {
        //                DocumentId = candidateMap[x.CandidateId].DocumentId,
        //                OnboardingDocId = x.Id,
        //                CandidateId = x.CandidateId,

        //                EmployeeId = candidateMap[x.CandidateId].EmployeeId,
        //                Name = candidateMap[x.CandidateId].Name,
        //                Designation = candidateMap[x.CandidateId].Designation,
        //                ContactNumber = candidateMap[x.CandidateId].ContactNumber,
        //                DOJ = candidateMap[x.CandidateId].DOJ,
        //                DOB = candidateMap[x.CandidateId].DOB,

        //                DocumentType = x.DocumentType != null
        //                    ? x.DocumentType.Label
        //                    : null,

        //                FileName = x.FileName,
        //                FilePath = x.FilePath
        //            })
        //            .ToListAsync();
        //    }

        //    return normalDocs
        //        .Concat(onboardingDocs)
        //        .ToList();
        //}
        //-------------------get grouping rule by cabinet--------------//
        public async Task<List<string>> GetCabinetGroupingColumns(int cabinetId)
        {
            var columns = await _context.CabinetGroupingRules
                .Where(c => c.Cabinet.CabinetId == cabinetId)
                .OrderBy(c => c.GroupingOrder)
                .Select(c => c.GroupingCol)
                .ToListAsync();

            if (!columns.Any())
                throw new KeyNotFoundException(
                    $"Configuration Error: No grouping rules defined for Cabinet ID {cabinetId}.");

            return columns;
        }


        //-------------------get columns of cabinet for upload--------------//
        public async Task<List<string>> GetCabinetUploadColumns(int cabinetId)
        {
            var columns = await _context.CabinetGroupingRules
                .Where(c => c.Cabinet.CabinetId == cabinetId && c.IsRequiredUpload == "true")
                .OrderBy(c => c.GroupingOrder)
                .Select(c => c.GroupingCol)
                .ToListAsync();

            if (!columns.Any())
                throw new KeyNotFoundException(
                    $"Configuration Error: No grouping rules defined for Cabinet ID {cabinetId}.");

            return columns;
        }
        //-------------------------NOTES---------------------------------------//

        //-----------CREATE-------------------
        public async Task<Notes> AddNoteAsync(Notes note)
        {

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        //---------UPDATE------------------

        public async void UpdateNote(Notes note)
        {
            _context.Notes.Update(note);
            await _context.SaveChangesAsync();
        }

        //-----------------DELETE-------------

        public async void DeleteNote(Notes note)
        {
            _context.Notes.Remove(note);
            //await _context.SaveChangesAsync();
        }

        //------------------GET NOTES BY DOC ID FOR NOTES ENDPOINT---------------
        public async Task<List<NotesDto>> GetDocumentWithNotesAsync(int documentId)
        {
            var notes = await _context.Notes
                    .Where(d => d.DocumentId == documentId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotesDto
                    {
                        NoteId = n.NoteId,
                        DocumentId = n.DocumentId,
                        NoteText = n.NoteText,
                        CreatedBy = n.CreatedBy,
                        CreatedAt = n.CreatedAt
                    })
                .ToListAsync();


            return notes;
        }
        public async Task<Notes> GetNoteByIdAsync(long id)
        {
            return await _context.Notes.FindAsync(id);
        }


        //------------------GET DOC ID FROM FILE PATH-------------------
        public async Task<int> GetDocumentIdFromPathAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new BadRequestException("File path is empty");

            // Normalize path (slashes)
            var normalizedPath = filePath.Replace("\\", "/");

            var doc = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.FilePath.Replace("\\", "/") == normalizedPath);

            return doc.DocumentId;
        }

        //--------DOCUMENT DOWNLOAD LINK----------------

        public async Task CreateDocDownloadLinkAsync(IEnumerable<DocDownloadLink> entities)
        {
            await _context.DocumentLink.AddRangeAsync(entities);

        }
        //to fetch all active document download links for a user in pdf viewer app
        public async Task<List<DocDownloadGetDTO>> GetAllDocumentForDownload(int? userid)
        {
            var docLink = await _context.DocumentLink
                .Where(d => d.AssignedTo == userid)
                .OrderByDescending(d => d.ExpiryDate)
                .Select(d => new DocDownloadGetDTO
                {
                    DocumentLinkId = d.Id,
                    DocumentId = d.DocumentId,
                    OnboardingDocId = d.OnboardingDocId,
                    ExpiresAt = d.ExpiryDate,
                    RemainingDownloads = d.MaxDownloads - d.CurrentDownloads,
                    // FileName = d.Document.FileName?
                    FileName = d.OnboardingDocId != null
                    ? d.OnboardingDocument!.FileName
                    : d.Document!.FileName
                })
                 .ToListAsync();

            return docLink;
        }

        public async Task<DocDownloadLink> GetByIdDownloadLinkAsync(int docid, int userid)
        {
            var docLink = await _context.DocumentLink
                .FirstOrDefaultAsync(d => d.DocumentId == docid && d.AssignedTo == userid);
            if (docLink == null)
                throw new NotFoundException("Download link not found.");
            return docLink;
        }

        //to determine the active document ids in the list for which download email have sent
        public async Task<List<int>> GetActiveDocumentIdsForUserAsync(int userId, IEnumerable<int> documentIds)
        {
            if (documentIds == null || !documentIds.Any())
                return new List<int>();

            return await _context.DocumentLink
                .Where(d =>
                    d.AssignedTo == userId &&
                    documentIds.Contains(d.DocumentId.Value) &&
                    d.ExpiryDate > DateTime.UtcNow && d.CurrentDownloads < d.MaxDownloads)
                .Select(d => d.DocumentId.Value)
                .ToListAsync();
        }

        //atomic  counter increment
        //public async Task<int> CounterDocumentDownload(DocumentRequestDto dto, int? userid)
        //{
        //    int rowsincremented;
        //    if (dto.Source== DocumentSourceType.Onboarding)
        //    {  

        //        rowsincremented = await _context.DocumentLink
        //        .Where(d =>
        //            d.OnboardingDocId == dto.Id &&
        //            d.AssignedTo == userid &&
        //            d.ExpiryDate > DateTime.UtcNow &&
        //            d.CurrentDownloads < d.MaxDownloads)
        //        .ExecuteUpdateAsync(setters =>
        //            setters.SetProperty(
        //                d => d.CurrentDownloads,
        //                d => d.CurrentDownloads + 1)
        //            .SetProperty(
        //                d => d.UpdatedAt,
        //                d => DateTime.UtcNow)
        //            );
        //        return rowsincremented;// can return count of updated rows , so can update more than 1 doc id
        //    }


        //    rowsincremented = await _context.DocumentLink
        //    .Where(d =>
        //        d.DocumentId == dto.Id &&
        //        d.AssignedTo == userid &&
        //        d.ExpiryDate > DateTime.UtcNow &&
        //        d.CurrentDownloads < d.MaxDownloads)
        //    .ExecuteUpdateAsync(setters =>
        //        setters.SetProperty(
        //            d => d.CurrentDownloads,
        //            d => d.CurrentDownloads + 1)
        //        .SetProperty(
        //            d => d.UpdatedAt,
        //            d => DateTime.UtcNow)

        //        );
        //    return rowsincremented;// can return count of updated rows , so can update more than 1 doc id
        //}


        public async Task<int> CounterDocumentDownload(DocumentRequestDto dto,int? userid)
        {
            var query = _context.DocumentLink
                .Where(d =>
                    d.AssignedTo == userid &&
                      d.IsActive &&
                    d.ExpiryDate > DateTime.UtcNow &&
                    d.CurrentDownloads < d.MaxDownloads);

            query = dto.Source == DocumentSourceType.Onboarding
                ? query.Where(d => d.OnboardingDocId == dto.Id)
                : query.Where(d => d.DocumentId == dto.Id);

            return await query.ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(
                        d => d.CurrentDownloads,
                        d => d.CurrentDownloads + 1)
                    .SetProperty(
                        d => d.UpdatedAt,
                        d => DateTime.UtcNow)
                .SetProperty(
                    d=>d.IsActive,
                    d=>d.CurrentDownloads+1<d.MaxDownloads
                ));
        }



        //---------------FIND DUPLICATE DOCUMENTS BASED ON CABINET RULES-----------------
        public async Task<Document?> FindDuplicateAsync(DocumentUploadDto dto, string? username, string? fullname)
        {
            if (!CabinetDuplicateRulesHelper.TryGetRules(dto.CabinetId, out var fields))
                return null;

            IQueryable<Document> query =
                _context.Documents.Where(d => d.CabinetId == dto.CabinetId);

            bool hasAnyFilter = false;
            dto.LoginId = username;
            dto.LoginName = fullname;
            foreach (var field in fields)
            {
                query = ApplyDuplicateFilter(query, field, dto, ref hasAnyFilter);

            }
            if (!hasAnyFilter)
                return null;

            return await query
                .OrderByDescending(d => d.Version)
                .FirstOrDefaultAsync();
        }

        private IQueryable<Document> ApplyDuplicateFilter(IQueryable<Document> query, string field, DocumentUploadDto dto, ref bool hasAnyFilter)
        {
            switch (field)
            {
                case "InvoiceNumber":
                    if (!string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.InvoiceNumber == dto.InvoiceNumber);
                    }
                    break;

                case "Amount":
                    if (dto.Amount != null)
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.Amount == dto.Amount);
                    }
                    break;

                case "InvoiceDate":
                    if (dto.InvoiceDate != null)
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.InvoiceDate == dto.InvoiceDate);
                    }
                    break;

                case "EmployeeId":
                    if (!string.IsNullOrWhiteSpace(dto.EmployeeId))
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.EmployeeId == dto.EmployeeId);
                    }
                    break;

                case "ContactNumber":
                    if (!string.IsNullOrWhiteSpace(dto.ContactNumber))
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.ContactNumber == dto.ContactNumber);
                    }
                    break;

                case "Name":
                    if (!string.IsNullOrWhiteSpace(dto.Name))
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.Name == dto.Name);
                    }
                    break;

                case "StatementDate":
                    if (dto.StatementDate != null)
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.StatementDate == dto.StatementDate);
                    }
                    break;

                case "ManufactureId":
                    if (dto.ManufactureId != null)
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.ManufactureId == dto.ManufactureId);
                    }
                    break;

                case "LoginId":
                    if (!string.IsNullOrWhiteSpace(dto.LoginId))
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.LoginId == dto.LoginId);
                    }
                    break;

                case "LoginName":
                    if (!string.IsNullOrWhiteSpace(dto.LoginName))
                    {
                        hasAnyFilter = true;
                        query = query.Where(d => d.LoginName == dto.LoginName);
                    }
                    break;

                case "Period":
                    if (!string.IsNullOrWhiteSpace(dto.Period))
                    {
                        hasAnyFilter = true;
                        DateOnly? period = DateFormatterHelper.ParsePeriod(dto.Period);
                        query = query.Where(d => d.Period == period);
                    }
                    break;
            }

            return query;
        }
        public bool IsDuplicate(Document dbDoc, DocumentMetadatadto record, string[] fields)
        {
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "InvoiceNumber":
                        if (!IsEqual(dbDoc.InvoiceNumber, record.InvoiceNumber)) return false;
                        break;

                    case "Amount":
                        if (!IsEqual(dbDoc.Amount, record.Amount)) return false;
                        break;

                    case "InvoiceDate":
                        if (!IsEqual(dbDoc.InvoiceDate, record.InvoiceDate)) return false;
                        break;

                    case "EmployeeId":
                        if (!IsEqual(dbDoc.EmployeeId, record.EmployeeId)) return false;
                        break;

                    case "ContactNumber":
                        if (!IsEqual(dbDoc.ContactNumber, record.ContactNumber)) return false;
                        break;

                    case "Name":
                        if (!IsEqual(dbDoc.Name, record.Name)) return false;
                        break;

                    case "StatementDate":
                        if (!IsEqual(dbDoc.StatementDate, record.StatementDate)) return false;
                        break;

                    case "ManufactureId":
                        if (!IsEqual(dbDoc.ManufactureId, record.ManufactureId)) return false;
                        break;

                    case "LoginId":
                        if (!IsEqual(dbDoc.LoginId, record.LoginId)) return false;
                        break;

                    case "LoginName":
                        if (!IsEqual(dbDoc.LoginName, record.LoginName)) return false;
                        break;

                    case "Period":
                        DateOnly? period = DateFormatterHelper.ParsePeriod(record.Period);
                        if (!IsEqual(dbDoc.Period, period)) return false;
                        break;
                }
            }

            return true;
        }
        private bool IsEqual<T>(T? a, T? b)
        {
            if (a == null || b == null)
                return false; // ignore nulls → no duplicate

            return EqualityComparer<T>.Default.Equals(a, b);
        }

        public async Task<List<Document>> GetDocumentsForDuplicateCheck(int cabinetId)
        {
            return await _context.Documents
                .Where(d => d.CabinetId == cabinetId && d.Status == "active")
                .ToListAsync();
        }

        public string GenerateDuplicateKeyFromDocument(Document doc, string[] fields)
        {
            var values = new List<string>();

            foreach (var field in fields)
            {
                object val = field switch
                {
                    "InvoiceNumber" => doc.InvoiceNumber ?? "",
                    "Amount" => doc.Amount?.ToString() ?? "",
                    "InvoiceDate" => doc.InvoiceDate?.ToString("yyyyMMdd") ?? "",
                    "EmployeeId" => doc.EmployeeId ?? "",
                    "ContactNumber" => doc.ContactNumber ?? "",
                    "Name" => doc.Name ?? "",
                    "StatementDate" => doc.StatementDate?.ToString("yyyyMMdd") ?? "",
                    "ManufactureId" => doc.ManufactureId?.ToString() ?? "",
                    "LoginId" => doc.LoginId ?? "",
                    "LoginName" => doc.LoginName ?? "",
                    "Period" => doc.Period?.ToString("yyyy-MM") ?? "",
                    _ => ""
                };
                values.Add(NormalizeValue(val));
            }

            return string.Join("|", values);
        }

        public string GenerateDuplicateKeyFromRecord(DocumentMetadatadto record, string[] fields)
        {
            var values = new List<string>();

            foreach (var field in fields)
            {
                object val = field switch
                {
                    "InvoiceNumber" => record.InvoiceNumber ?? "",
                    "Amount" => record.Amount?.ToString() ?? "",
                    "InvoiceDate" => record.InvoiceDate?.ToString("yyyyMMdd") ?? "",
                    "EmployeeId" => record.EmployeeId ?? "",
                    "ContactNumber" => record.ContactNumber ?? "",
                    "Name" => record.Name ?? "",
                    "StatementDate" => record.StatementDate?.ToString("yyyyMMdd") ?? "",
                    "ManufactureId" => record.ManufactureId?.ToString() ?? "",
                    "LoginId" => record.LoginId ?? "",
                    "LoginName" => record.LoginName ?? "",
                    "Period" => record.Period ?? "",
                    _ => ""
                };
                values.Add(NormalizeValue(val));
            }

            return string.Join("|", values);

        }

        private string NormalizeValue(object value)
        {
            if (value == null)
                return "";


            if (value is decimal dec)
                return dec.ToString("0.00");

            if (value is double dbl)
                return dbl.ToString("0.00");


            if (value is string str)
            {
                str = str.Trim();


                if (decimal.TryParse(str, out var parsed))
                    return parsed.ToString("0.00");

                return str.ToLower();
            }

            if (value is DateTime dt)
                return dt.ToString("yyyyMMdd");

            return value.ToString().Trim().ToLower();
        }

        public async Task<List<ManfactureDto>> GetManufactureDetailsList()
        {
            return await _context.ManufactureDetails
                .Where(d => d.ManufactureId != null)
                .Select(d => new ManfactureDto
                {
                    Id = d.Id,
                    ManfactureId = d.ManufactureId,
                    ManfactureName = d.ManufactureName
                })
                .Distinct()
                .ToListAsync();
        }
        
    }
}
