using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.Document;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;
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
            await _context.SaveChangesAsync();
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
                 .Include(d => d.DocumentType)
                 .OrderBy(d => d.UploadedAt)
                .AsQueryable();
        }

        // ---------------- GET LATEST VERSION -----------------
        public async Task<int> GetLatestVersion(int cabinetId, string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);

            var versions = await _context.Documents
                .Where(d => d.CabinetId == cabinetId &&
                            d.FileName.StartsWith(baseName))
                .OrderByDescending(d => d.Version)
                .Select(d => d.Version)
                .FirstOrDefaultAsync();

            return versions; // if no version, default = 0
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
        public async Task DeleteDocument(int documentId)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc == null)
                throw new Exception("Document not found");
            else
            {
                //_context.Documents.Remove(doc);
                doc.Status = "archived";
                _context.Documents.Update(doc);
                await _context.SaveChangesAsync();

            }
        }

        //---------------------File explorer -----------------------
        public async Task<List<DocumentFileExplorer>> GetFileExplorerAsync(int cabinetId)
        {
            var files = await _context.Documents
        .Where(d => d.CabinetId == cabinetId && d.Status == "active")
        .Select(d => new DocumentFileExplorer
        {
            DocumentId = d.DocumentId,
            FileName = d.FileName,
            FilePath = d.FilePath
        })
        .ToListAsync();

            return files;
        }


        //-----------GET NOTES BY DOC ID FROM DB------------------
        public override async Task<Document?> GetByIdAsync(int id)
        {
            return await Query()
                .Include(d => d.Notes)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
        }
        //------------  GET DOC TYPES-------------

        public async Task<List<string>> GetDocTypesAsync()
        {
            var doctype = await _context.DocumentTypes
                .Select(d => d.Label)
                .Distinct()
                .ToListAsync();
            if (doctype == null)
                throw new Exception("Document type not found");

            return doctype;
        }

        //---------------GET DOC TYPE DETAILS BY doc_type name---------------

        public async Task<DocumentTypes> GetOrCreateDocLabelAsync(string label)//can be used for filtering as it is dropdown
        {
            label = label.Trim();

            var existing = await _context.DocumentTypes
                .FirstOrDefaultAsync(x => x.Label == label);

            if (existing != null)
                return existing;

            var docType = new DocumentTypes
            {
                Key = GenerateDocKeyHelper.GenerateDocKey(label),
                Label = label,
                Status = true
            };

            _context.DocumentTypes.Add(docType);
            await _context.SaveChangesAsync();
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
        public async Task<List<DocDownloadGetDTO>> GetAllDocumentForDownload(int userid)
        {
            var docLink = await _context.DocumentLink
                .Where(d => d.UserId == userid)
                .Where(d => d.ExpiryDate > DateTime.UtcNow)
                .Where(d=>d.CurrentDownloads<d.MaxDownloads)
                .OrderBy(d => d.ExpiryDate)
                .Select(d => new DocDownloadGetDTO
                {
                    DocumentId = d.DocumentId,
                    ExpiresAt = d.ExpiryDate,
                    RemainingDownloads = d.MaxDownloads-d.CurrentDownloads,
                    FileName = d.Document.FileName
                })
                 .ToListAsync();
            //if (docLink == null || !docLink.Any())
            //    throw new NotFoundException("No download link found for the user.");
            return docLink;
        }

        public async Task<DocDownloadLink> GetByIdDownloadLinkAsync(int docid, int userid)
        {
            var docLink = await _context.DocumentLink
                .FirstOrDefaultAsync(d => d.DocumentId == docid && d.UserId == userid);
            if (docLink == null)
                throw new NotFoundException("Download link not found.");
            return docLink;
        }

        //to determine the active document ids in the list for which download email have sent
        public async Task<List<int>> GetActiveDocumentIdsForUserAsync(int userId,IEnumerable<int> documentIds)
        {
            if (documentIds == null || !documentIds.Any())
                return new List<int>();

            return await _context.DocumentLink
                .Where(d =>
                    d.UserId == userId &&
                    documentIds.Contains(d.DocumentId) &&
                    d.ExpiryDate > DateTime.UtcNow && d.CurrentDownloads<d.MaxDownloads)
                .Select(d => d.DocumentId)
                .ToListAsync();
        }

        //atomic  counter increment
        public async Task<int> CounterDocumentDownload(int docid, int userid)
        {
            var rowsincremented = await _context.DocumentLink
            .Where(d =>
                d.DocumentId == docid &&
                d.UserId == userid &&
                d.ExpiryDate > DateTime.UtcNow &&
                d.CurrentDownloads < d.MaxDownloads)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    d => d.CurrentDownloads,
                    d => d.CurrentDownloads + 1)
                .SetProperty(
                    d => d.UpdatedAt,
                    d => DateTime.UtcNow)

                );
            return rowsincremented;// can return count of updated rows , so can update more than 1 doc id
        }
    }
}
