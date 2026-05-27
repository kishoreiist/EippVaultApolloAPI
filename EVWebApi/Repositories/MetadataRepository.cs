using EVWebApi.Data;
using Microsoft.EntityFrameworkCore;
using EVWebApi.Models;
using EVWebApi.Interfaces.Repositories;
namespace EVWebApi.Repositories
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly AppDbContext _context;

        public MetadataRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddMetadata(List<Metadata> metadata)
        {
            await _context.Metadata.AddRangeAsync(metadata);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Metadata>> GetMetadataByDocumentId(int documentId)
        {
            return await _context.Metadata
                .Where(m => m.DocumentId == documentId)
                .ToListAsync();
        }

        public async Task DeleteMetadataByDocumentId(int documentId)
        {
            var items = await _context.Metadata
                .Where(m => m.DocumentId == documentId)
                .ToListAsync();

            _context.Metadata.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
