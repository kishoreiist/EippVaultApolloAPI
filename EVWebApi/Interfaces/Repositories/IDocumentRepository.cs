using EVWebApi.DTOs.Document;
using EVWebApi.Models;
using System.Xml.Linq;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IDocumentRepository : IGenericRepository<Document>
    {
        Task<Document> CreateDocument(Document doc);
        Task<Document> GetDocument(int id);
        Task<int> GetLatestVersion(int docId);
        Task UpdateStatus(int id);
        Task DeleteDocument(int documentId);
        IQueryable<Document> Query();
        Task<List<DocumentFileExplorer>> GetFileExplorerAsync(int cabinetId);

        Task<List<string>> GetDocTypesAsync();
        Task<DocumentTypes> GetOrCreateDocLabelAsync(string label);
        Task<string> GetDocumentName(int id);
        void AddDocumentRange(Document doc);
        Task<List<string>> GetCabinetGroupingColumns(int cabinetId);
        Task<List<Document>> GetDocumentsByIds(List<int> ids);
        Task<List<Document>> ExcelExportQuery(ExportExcelDocDto dto);
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

        Task<int> CounterDocumentDownload(int docid, int? userid);


        ///--versioning
      
        Task<Document?> FindDuplicateAsync(DocumentUploadDto dto);
        Task<List<Document>> GetDocumentsForDuplicateCheck(int cabinetId);
        bool IsDuplicate(Document dbDoc, DocumentMetadatadto record, string[] fields);
        string GenerateDuplicateKeyFromDocument(Document doc, string[] fields);
        string GenerateDuplicateKeyFromRecord(DocumentMetadatadto record, string[] fields);

    }
}
