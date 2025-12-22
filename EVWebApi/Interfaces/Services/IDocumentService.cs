using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Pagination;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto,int CurrentUserId);
        Task<DocumentResponseDto> GetDocument(int id);
        Task<PagedResponse<DocumentResponseDto>> GetDocumentsByCabinetId(int cabinetId, DocumentQueryParameters query);
        Task<DocumentResponseDto> UpdateDocumentAsync(int id,UpdateDocumentDto dto);
        Task<Stream?> GetDocumentStream(int id);
        Task<DocumentDownloadDto?> GetDocumentForDownload(int id);
        Task<(int cabinetId, bool status)> DeleteDocument(int id);
        Task<List<DocumentFileExplorer>> GetFileExplorerDocumentAsync(int id);

        Task<List<string>> GetDocTypeAsync();
        //Task ArchiveDocument(int id);
        //Task RestoreDocument(int id);
        //--------------NOTES------------
        Task<List<NotesDto>> GetDocumentWithNotesAsync(int id);

        Task<NotesDto> CreateNoteAsync(NoteCreateDto dto, string CurrentUsername);
        Task<NotesDto> UpdateNoteAsync(NoteUpdateDto dto);
        Task<string> DeleteNoteAsync(long noteId);
    }
}
