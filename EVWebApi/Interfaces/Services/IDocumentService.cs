using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto,int CurrentUserId);
        Task<DocumentResponseDto> GetDocument(int id);
        Task<PagedResponse<DocumentResponseDto>> GetDocumentsByCabinetId(int cabinetId, DocumentQueryParameters query);
        Task<PagedResponse<GroupedDocResponseDTO>> GetGroupedDocuments(int cabinetId, DocumentQueryParameters query);
        Task<DocumentResponseDto> UpdateDocumentAsync(int id,UpdateDocumentDto dto);
        Task<DocumentStreamResultDTO?> GetDocumentStream(int id);
        Task<DocumentDownloadDto?> GetDocumentForDownload(int id);
        Task<(int cabinetId, bool status)> DeleteDocument(int id);
        Task<BatchResponseDTO> DeleteMultipleDocuments(List<int> ids);
        Task<Stream?> GetMergedDocumentStream(List<int> documentIds);
        Task<(Stream ZipStream, string ZipFileName)> GetZIPFile(BatchDocDto dto);
        Task<List<DocumentFileExplorer>> GetFileExplorerDocumentAsync(int id);
        Task<List<string>> GetDocTypeAsync();

        Task<BatchResponseDTO> BatchUploadDocuments(BatchUploadDTO dto, int currentuserid);
        Task<DocumentResponseDto?> UploadDocumentChunks(DocumentUploadDto dto, int currentuserid);
        //Task ArchiveDocument(int id);
        //Task RestoreDocument(int id);
        //--------------NOTES------------
        Task<List<NotesDto>> GetDocumentWithNotesAsync(int id);

        Task<NotesDto> CreateNoteAsync(NoteCreateDto dto, string CurrentUsername);
        Task<NotesDto> UpdateNoteAsync(NoteUpdateDto dto);
        Task<string> DeleteNoteAsync(long noteId);
    }
}
