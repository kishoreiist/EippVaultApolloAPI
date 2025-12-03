using EVWebApi.Data;
using EVWebApi.DTOs.Document;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        private readonly AppDbContext _context;

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

        // ---------------- GET DOCUMENT BY Doc ID----------------------
        public async Task<Document> GetDocument(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.MetadataList)
                .Include(d => d.Notes)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null)
                throw new Exception("Document not found");

            return doc;
        }
        //--------------GET DOCUMENT BY Cabinet ID------------------
        public IQueryable<Document> Query()
        {
            return _context.Documents
                .Include(d => d.Notes).AsQueryable();
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
       //-----------------------------GET NOTES BY DOC ID FROM DB-----------------------------------------
        public override async Task<Document?> GetByIdAsync(int id)
        {
            return await Query()
                .Include(d => d.Notes)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
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
            //await _context.SaveChangesAsync();
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
    }
}
