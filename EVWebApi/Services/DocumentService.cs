using EVWebApi.DTOs;
using EVWebApi.DTOs.Document;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Repositories;

namespace EVWebApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        private readonly IWebHostEnvironment _env;
        private readonly IMetadataRepository _metadataRepo;

        public DocumentService(IDocumentRepository repo, IMetadataRepository metadataRepo, IWebHostEnvironment env)
        {
            _repo = repo;
            _metadataRepo = metadataRepo;
            _env = env;
        }

        // ---------------------- UPLOAD ----------------------
        public async Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto)
        {
            // Create folder if not exist
            string folderPath = Path.Combine(_env.ContentRootPath, "Uploads", "Documents", dto.CabinetId.ToString());
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Versioning logic
            int version = await _repo.GetLatestVersion(dto.CabinetId, dto.File.FileName) + 1;

            // Create unique filename
            string fileName = $"{Path.GetFileNameWithoutExtension(dto.File.FileName)}_v{version}{Path.GetExtension(dto.File.FileName)}";
            string filePath = Path.Combine(folderPath, fileName);

            // Save file physically
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // Save to DB
            var doc = await _repo.CreateDocument(new Document
            {
                CabinetId = dto.CabinetId,
                FileName = fileName,
                FilePath = filePath,
                UploadedBy = dto.UploadedBy,
                Version = version,
                Status = "active"
            });
            if (dto.Metadata != null)
            {
                foreach (var item in dto.Metadata)
                {
                    await _metadataRepo.AddMetadata(new Metadata
                    {
                        DocumentId = doc.DocumentId,
                        MetaKey = item.Key,
                        MetaValue = item.Value
                    });
                }
            }

            return new DocumentResponseDto
            {
                DocumentId = doc.DocumentId,
                FileName = doc.FileName,
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

        // ---------------------- GET DOCUMENT ----------------------
        public async Task<DocumentResponseDto> GetDocument(int id)
        {
            var doc = await _repo.GetDocument(id);
            var metadata = await _metadataRepo.GetMetadataByDocumentId(id);

            return new DocumentResponseDto
            {
                DocumentId = doc.DocumentId,
                FileName = doc.FileName,
                Version = doc.Version,
                UploadedAt = doc.UploadedAt,
                Status = doc.Status,
                Metadata = metadata.Select(m => new MetadataDTO
                {
                    Key = m.MetaKey,
                    Value = m.MetaValue
                }).ToList()
            };
        }

        // ---------------------- PREVIEW STREAM ----------------------
        public async Task<Stream?> GetDocumentStream(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null) return null;

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, doc.FilePath.TrimStart('/').Replace("/", "\\"));

            if (!File.Exists(fullPath))
                return null;

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        }

        // ---------------------- DOWNLOAD ----------------------
        public async Task<DocumentDownloadDto?> GetDocumentForDownload(int id)
        {
            var doc = await _repo.GetDocument(id);
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, doc.FilePath.TrimStart('/').Replace("/", "\\"));

            if (!File.Exists(fullPath))
                return null;

            var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new DocumentDownloadDto { Stream = fs, FileName = doc.FileName };
        }

        // ---------------------- ARCHIVE ----------------------
        public async Task ArchiveDocument(int id)
        {
            await _repo.UpdateStatus(id, "archived");
        }

        // ---------------------- RESTORE ----------------------
        public async Task RestoreDocument(int id)
        {
            await _repo.UpdateStatus(id, "active");
        }
        // ------------------DELETE--------------------
        public async Task<bool> DeleteDocument(int id)
        {
            var doc = await _repo.GetDocument(id);
            if (doc == null)
                return false;

            // Delete metadata
            await _metadataRepo.DeleteMetadataByDocumentId(id);
            string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace("/", "\\"));
            // Delete physical file
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            // Delete DB record
            await _repo.DeleteDocument(id);

            return true;
        }


    }
}
