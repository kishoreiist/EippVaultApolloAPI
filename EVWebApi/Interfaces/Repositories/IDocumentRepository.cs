using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.HR;
using EVWebApi.Models;
using EVWebApi.Models.HR;
using System.Xml.Linq;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IDocumentRepository : IGenericRepository<Document>
    {
        Task<Document> CreateDocument(Document doc);
        Task<Document> GetDocument(int id);
        Task<int> GetLatestVersion(int docId);
        Task UpdateStatus(int id);
        Task<Document> DeleteDocument(int documentId);
        IQueryable<Document> Query();
        Task<List<DocumentFileExplorer>> GetFileExplorerAsync(int cabinetId);

        Task<List<DocTypeCreateDto>> GetDocTypesAsync();
        Task<DocumentTypes> GetOrCreateDocLabelAsync(DocTypeCreateDto dto);
        Task<DocumentTypes> GetDocTypeDetailsByNameAsync(string name);
        Task<string> GetDocumentName(int id);
        void AddDocumentRange(Document doc);
        Task<List<string>> GetCabinetGroupingColumns(int cabinetId);
        Task<List<string>> GetCabinetUploadColumns(int cabinetId);
        Task<List<Document>> GetDocumentsByIds(List<int> ids);
        Task<List<Document>> ExcelExportQuery(ExportExcelDocDto dto);
        //Task<List<DocumentExportDto>> ExcelExportQuery(ExportExcelDocDto dto);
        Task<int> GetDocumentIdFromPathAsync(string filePath); //need to check
        //--------NOTES----------------
        Task<List<NotesDto>> GetDocumentWithNotesAsync(int documentId);
        Task<Notes> AddNoteAsync(Notes note);
        void  UpdateNote(Notes note);
        void DeleteNote(Notes note);
        Task<Notes> GetNoteByIdAsync(long id);
        //----------------------- doc download link----------------

        Task CreateDocDownloadLinkAsync(IEnumerable<DocDownloadLink> entities);
        Task<List<DocDownloadGetDTO>> GetAllDocumentForDownload(int? userid);

        Task<DocDownloadLink>GetByIdDownloadLinkAsync(int docid,int userid);
        Task<List<int>> GetActiveDocumentIdsForUserAsync(int userId, IEnumerable<int> documentIds);

        Task<int> CounterDocumentDownload(DocumentRequestDto dto, int? userid);


        ///--versioning

        Task<Document?> FindDuplicateAsync(DocumentUploadDto dto, string? username, string? fullname);
        Task<List<Document>> GetDocumentsForDuplicateCheck(int cabinetId);
        bool IsDuplicate(Document dbDoc, DocumentMetadatadto record, string[] fields);
        string GenerateDuplicateKeyFromDocument(Document doc, string[] fields);
        string GenerateDuplicateKeyFromRecord(DocumentMetadatadto record, string[] fields);


        Task<List<ManfactureDto>> GetManufactureDetailsList();
    

    }
}
