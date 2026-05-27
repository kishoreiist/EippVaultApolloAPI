using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace EVWebApi.Repositories
{
    public class DocVersionRepository : GenericRepository<DocumentVersion>, IDocVersionRepository
    {

        private new readonly AppDbContext _context;

        public DocVersionRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<DocumentVersion> GetLatestVersionAsync(int id)
        {
            var version = await _context.DocumentVersion.
                Where(v => v.DocumentId == id).
                OrderByDescending(v => v.VersionId).
                FirstOrDefaultAsync();

            return version;
        }
        public async Task<List<DocumentVersion>> GetVersionsAsync(int documentId)
        {
            var versions = await _context.DocumentVersion
                .Where(v => v.DocumentId == documentId && v.Status=="active")
                .OrderByDescending(v => v.UploadedAt)
                .ToListAsync();

            return versions;
        }

        public async Task GetOldVersionsToDelete(int docId, int keepCount)
        {
            var entities= await _context.DocumentVersion
                .Where(v => v.DocumentId == docId)
                .OrderByDescending(v => v.VersionId) // latest first
                .Skip(keepCount) // skip latest 5
                .ToListAsync();
            foreach (var v in entities)
            {
                v.Status = "archived";
            }
            //await DeleteOldArchivedVersions(docId);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOldArchivedVersions(int docId)
        {
            var entities = await GetArchivedVersions(docId);
            _context.DocumentVersion.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DocumentVersion>> GetArchivedVersions(int docId)
        {
            return await _context.DocumentVersion
                .Where(v => v.DocumentId == docId && v.Status == "archived" && v.Action!="modified")
                .OrderByDescending(v => v.VersionId)
                .Skip(5)
                .ToListAsync();
        }
        //-------------------------locks----------------------
        public async Task<DocumentLock?> GetLockByDocumentIdAsync(int documentId)
        {
            return await _context.DocumentLock
                .OrderByDescending(d=>d.LockExpiry)
                .FirstOrDefaultAsync(l => l.DocumentId == documentId);
        }

        public void RemoveLock(DocumentLock entity)
        {
            _context.DocumentLock.Remove(entity);
        }

        public async Task AddLockAsync(DocumentLock entity)
        {
            await _context.DocumentLock.AddAsync(entity);
        }
    }
}
