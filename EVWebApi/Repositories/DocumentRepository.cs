using EVWebApi.Data;
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
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null)
                throw new Exception("Document not found");

            return doc;
        }
        //--------------GET DOCUMENT BY Cabinet ID------------------
        public IQueryable<Document> Query()
        {
            return _context.Documents.AsQueryable();
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
        public async Task UpdateStatus(int id, string status)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                throw new Exception("Document not found");

            doc.Status = status;
            await _context.SaveChangesAsync();
        }
        // ---------------- DELETE -------------------
        public async Task DeleteDocument(int documentId)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc != null)
            {
                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync();
            }
        }

    }
}
