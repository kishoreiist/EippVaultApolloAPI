using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocumentService
    {
        //Task<DocumentResponseDto> UploadDocument(DocumentUploadDto dto,int? CurrentUserId);
        Task<DocumentResponseDto> GetDocument(int id);
        Task<PagedResponse<DocumentResponseDto>> GetDocumentsByCabinetId(int cabinetId, DocumentQueryParameters query);
        Task<GroupedPaginationResponse<GroupedDocResponseDTO>> GetGroupedDocuments(int cabinetId, DocumentQueryParameters query);
        Task<DocumentResponseDto> UpdateDocumentAsync(int id,UpdateDocumentDto dto,int? userId);
        Task<DocumentStreamResultDTO?> GetDocumentStream(int id);
        Task<DocumentDownloadDto?> GetDocumentForDownload(int id);
        Task<(int cabinetId, bool status)> DeleteDocument(int id);
        Task<BatchResponseDTO> DeleteMultipleDocuments(List<int> ids);
        Task<Stream?> GetMergedDocumentStream(List<int> documentIds);
        Task<(Stream ZipStream, string ZipFileName)> GetZIPFile(BatchDocDto dto);
        Task<(byte[] Excel, string FileName)> GetExportExcel(ExportExcelDocDto dto);
        Task<List<DocumentFileExplorer>> GetFileExplorerDocumentAsync(int id);
        Task<List<string>> GetDocTypeAsync();

        Task<BatchResponseDTO> BatchUploadDocuments(BatchUploadDTO dto, int? currentuserid);
        Task<DocumentResponseDto?> UploadDocumentChunks(DocumentUploadDto dto, int? currentuserid);
        Task<BatchResponseDTO> ApplyExcelPatchAsync(ExcelPatchRequestDto dto, int? userId);

        Task<List<string>> GetSuggestionsAsync(AutoSuggestionRequestDto dto);
        Task<object> GetAutoFillAsync(AutoFillRequestDto dto);

        Task<DocumentResponseDto> SplitAndExtractPdfAsync(SplitAndExtractPdfDto dto, int? userId);
        //Task ArchiveDocument(int id);
        //Task RestoreDocument(int id);
        //--------------NOTES------------
        Task<List<NotesDto>> GetDocumentWithNotesAsync(int id);

        Task<NotesDto> CreateNoteAsync(NoteCreateDto dto, string CurrentUsername);
        Task<NotesDto> UpdateNoteAsync(NoteUpdateDto dto);
        Task<string> DeleteNoteAsync(long noteId);

        //--------------DOCUMENT DOWNLOAD LINK------------
        //Task IncrementDownloadCountAsync(int linkId);
        //Task<DocDownloadLink?> ValidateLinkAsync(string token, string? password = null);
        //Task<DownloadLinkDto> CreateLinkAsync(int documentId, int expiresInDays = 3, int maxDownloads = 2);


        Task<List<DocDownloadGetDTO>> GetAllDocumentForDownloadAsync(int? userid);

        Task<DocumentStreamResultDTO?> GenerateProtectedDownloadAsync(int docid, int? userid);

        Task<List<ListDto>> GetExcelSheetNamesAsync(int documentId);
        Task<string> OpenExcelSheetAsync(DocumentExcelOpenDTO dto);


        Task MergeUploadFileAsync(string existingFilePath, IFormFile newFile, string outputPath);
    }
}
