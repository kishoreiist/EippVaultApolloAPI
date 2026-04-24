using DocumentFormat.OpenXml.Drawing;
using EVWebApi.Data;
using EVWebApi.DTOs.HR;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models.HR;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using System.Linq.Dynamic.Core.Tokenizer;

namespace EVWebApi.Repositories
{
    public class ConfigurationRepository : GenericRepository<DocumentCollection>, IConfigurationRepository
    {
        private new readonly AppDbContext _context;

        public ConfigurationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<DocumentCollection?> GetCollectionByNameAsync(string name)
        {
            var normalized = name.Trim().ToLower();

            return await _context.DocumentCollections
                .FirstOrDefaultAsync(c => c.Name.ToLower() == normalized);
        }

        public async Task<DocumentCollection?> GetCollectionByIdAsync(int id)
        {
            return await _context.DocumentCollections
                .Include(c => c.CollectionDocumentTypes)
                .ThenInclude(cd => cd.DocumentType)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public IQueryable<DocumentCollection> Query()
        {
            return _context.DocumentCollections
                .Include(c => c.CollectionDocumentTypes)
                .ThenInclude(cd => cd.DocumentType)
                .OrderByDescending(cd => cd.Id)
                .AsNoTracking();
        }

        public async Task<ConfigRequestRecipient?> GetConfigRequestByToken(string token)
        {
            return await _context.ConfigurationRequestRecipient
                .Include(r => r.Request)
                    .ThenInclude(req => req.Collection)
                        .ThenInclude(c => c.CollectionDocumentTypes)
                            .ThenInclude(cd => cd.DocumentType)
                .Include(r => r.UploadedDocuments)

                .FirstOrDefaultAsync(r => r.Token == token);
        }

        public IQueryable<ConfigRequest> GetConfigListAsync()
        {
            return _context.ConfigurationRequests
                .Include(r => r.Collection)
                .Include(r => r.Recipients)
                .OrderByDescending(r=>r.Id)
                .AsQueryable();

        }
        public async Task<ConfigRequest?> GetConfigRequestByIdAsync(int id, string? status)
        {

            var statusFilter = string.IsNullOrEmpty(status) ? "Completed" : status;

            //return await _context.ConfigurationRequests
            //    .Include(r => r.Collection)
            //        .ThenInclude(c => c.CollectionDocumentTypes)
            //    .Include(r => r.Recipients
            //        .Where(r => r.Status == statusFilter))
            //    .FirstOrDefaultAsync(r => r.Id == id);


            return await _context.ConfigurationRequests
                 .Where(r => r.Id == id)
                 .Select(r => new ConfigRequest
                 {
                     Id = r.Id,
                     Collection = r.Collection,
                     Recipients = r.Recipients
                         .Where(rec => rec.Status == statusFilter)
                         .ToList()
                 })
                 .FirstOrDefaultAsync();
        }

        public async Task<int?>GetUploadCount(int recipientId)
        {
            return await _context.OnboardingHRDocument
               .Where(x => x.RecipientId == recipientId)
               .Select(x => x.DocumentTypeId)
               .Distinct()
               .CountAsync();
        }
    }
}
