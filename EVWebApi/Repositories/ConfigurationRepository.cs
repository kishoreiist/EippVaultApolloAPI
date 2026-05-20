using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.Excel;
using EVWebApi.Data;
using EVWebApi.DTOs.HR;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using EVWebApi.Models.HR;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using System.Drawing;
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
                .Include(r => r.Candidate)
                .FirstOrDefaultAsync(r => r.Token == token);
        }

        public IQueryable<ConfigRequest> GetConfigListAsync()
        {
            return _context.ConfigurationRequests
                .Include(r => r.Collection)
                .Include(r => r.Recipients)
                    .ThenInclude(r => r.Candidate)
                .OrderByDescending(r=>r.Id)
                .AsQueryable();

        }

        //---need to addd
        public async Task<List<ConfigRequest>> GetConfigRequestAsync(ConfigQueryDetailDto dto)
        {
            var statusFilter = string.IsNullOrEmpty(dto.Status) ? "completed" : dto.Status;
            var typeFilter = string.IsNullOrEmpty(dto.Type)? "pre": dto.Type;
            return await _context.ConfigurationRequests
                .Where(cr =>
                 (string.IsNullOrEmpty(dto.Region) || cr.Collection.Region == dto.Region)
                 &&
                (cr.Collection.Type == typeFilter)
                 &&
                 (cr.Recipients.Any(r => r.Status == statusFilter)))
                .Include(r => r.Collection)
                    .ThenInclude(c => c.CollectionDocumentTypes)
                        .ThenInclude(cd => cd.DocumentType)
                .Include(r => r.Recipients
                    .Where(r => r.Status == statusFilter && r.Candidate.Status == "active" && r.Candidate.IsHired != true))   
                    .ThenInclude(r => r.Candidate)      
                .ToListAsync();
        }

        

        public async Task<int?>GetUploadCount(int recipientId,int candidateId)
        {
            return await _context.OnboardingHRDocument
               .Where(x => x.RecipientId == recipientId && x.CandidateId == candidateId)
               .Select(x => x.DocumentTypeId)
               .Distinct()
               .CountAsync();
        }

        public async Task<OnboardingDocument> GetOnboardingFilesAsync(int docid)
        {
            var doc = await _context.OnboardingHRDocument
                .Include(d=>d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == docid);

            if (doc == null || doc.Status== "archived")
                throw new Exception("Document not found");


            return doc;
        }

        public async Task<HrConfirmationBatch> CreateOnboardingBatch(HrConfirmationBatch batch)
        {
            _context.HrConfirmationBatches.Add(batch);
            
            return batch;
        }

        public async Task<HrConfirmationBatchRow> CreateOnboardingBatchRows(HrConfirmationBatchRow batchrows)
        {
            _context.HrConfirmationBatchRows.Add(batchrows);
            
            return batchrows;
        }

        //public async Task<ConfigRequestRecipient?> MatchOnboardingCandidateAsync(HrParsedRowDto row)
        //{
        //    var normalizedEmail = NormalizeInput(row.Email);
        //    var normalizedPan = NormalizeInput(row.PAN);
        //    var normalizedAadhaar = NormalizeInput(row.Aadhaar);

        //    var query = _context.ConfigurationRequestRecipient
        //        .Where(x =>
        //            x.Status == "completed" ||
        //            x.Status == "inProgress");

        //    var candidates = await query.ToListAsync();

        //    return candidates.FirstOrDefault(x =>

        //        (!string.IsNullOrWhiteSpace(normalizedEmail) &&
        //         NormalizeInput(x.Email) == normalizedEmail)

        //        &&

        //        (!string.IsNullOrWhiteSpace(normalizedPan) &&
        //         NormalizeInput(x.PAN) == normalizedPan)

        //        &&

        //        (!string.IsNullOrWhiteSpace(normalizedAadhaar) &&
        //         NormalizeInput(x.Adhaar) == normalizedAadhaar)
        //    );
        //}
        public async Task<ConfigRequestRecipient?> MatchOnboardingCandidateAsync(HrParsedRowDto row)
        {
            var normalizedEmail = NormalizeInput(row.Email);
            var normalizedPan = NormalizeInput(row.PAN);
            var normalizedAadhaar = NormalizeInput(row.Aadhaar);

            var candidates = await _context.ConfigurationRequestRecipient
                .Where(x =>
                    x.Status == "completed" ||
                    x.Status == "inProgress")
                .Include(x=>x.Candidate)
                .ToListAsync();

            return candidates.FirstOrDefault(x =>

                NormalizeInput(x.Candidate.Email) == normalizedEmail &&

                NormalizeInput(x.Candidate.PAN) == normalizedPan &&

                NormalizeInput(x.Candidate.Adhaar) == normalizedAadhaar
            );
        }

        public async Task<string>GetOnboardingFileNameById(int id)
        {
                return await _context.OnboardingHRDocument
                    .Where(d => d.Id == id)
                    .Select(d => d.FileName)
                    .FirstOrDefaultAsync() ?? throw new Exception("Document not found");
        }

        public async Task<List<int>> GetActiveOnboardDocIdsForUserAsync(int userId, IEnumerable<int> documentIds)
        {
            if (documentIds == null || !documentIds.Any())
                return new List<int>();

            return await _context.DocumentLink
                .Where(d =>
                    d.AssignedTo == userId &&
                    documentIds.Contains(d.OnboardingDocId.Value) &&
                    d.ExpiryDate > DateTime.UtcNow && d.CurrentDownloads < d.MaxDownloads)
                .Select(d => d.OnboardingDocId.Value)
                .ToListAsync();
        }
        public async Task<OnboardingDocument> DeleteOnboardingDocument(int id)
        {
            var doc = await _context.OnboardingHRDocument.FindAsync(id);
            if (doc == null || doc.Status=="archived")
                throw new Exception("Document not found");
            else
            {
                //_context.Documents.Remove(doc);
                doc.Status = "archived";
                _context.OnboardingHRDocument.Update(doc);
                await _context.SaveChangesAsync();
                return doc;
            }
        }

        public async Task<List<int>> GetExisitngCandidatesByEmail(List<string> emails)
        {
            if (emails == null || !emails.Any())
                return new List<int>();
            var normalizedEmails = emails.Select(NormalizeInput).ToList();
            return await _context.Candidates
                .Where(r => normalizedEmails.Contains(NormalizeInput(r.Email)))
                .Select(r => r.Id)
                .ToListAsync();
        }

        public async Task<Candidate> GetCandidateByIdAsync(int id)
        {
            return await _context.Candidates.FindAsync(id) ?? throw new NotFoundException("Candidate not found");
        }

        public async Task<ConfigRequestRecipient?> GetRecipientReqByCandidateId(int candidateId)
        {
            return await _context.ConfigurationRequestRecipient
                .Include(x => x.Request)
                    .ThenInclude(r => r.Collection)
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId &&
                    x.Request.Collection.Type.ToLower() == "pre");
        }
        //-----------------------------helpers--------------------------
        private static string NormalizeInput(string? value)
        {
            return value?
                .Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .ToUpperInvariant()
                ?? string.Empty;
        }
    }
}
