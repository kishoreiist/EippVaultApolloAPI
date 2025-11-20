using EVWebApi.DTOs;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto);
        Task<DocumentResponseDto> GetDocument(int id);
        Task<Stream?> GetDocumentStream(int id);
        Task<DocumentDownloadDto?> GetDocumentForDownload(int id);
        Task ArchiveDocument(int id);
        Task RestoreDocument(int id);
        Task<bool> DeleteDocument(int id);
    }
}
