using EVWebApi.DTOs.Document;
using EVWebApi.Models;
using System.Xml.Linq;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IDocumentRepository : IGenericRepository<Document>
    {
        Task<Document> CreateDocument(Document doc);
        Task<Document> GetDocument(int id);
        Task<int> GetLatestVersion(int cabinetId, string fileName);
        Task UpdateStatus(int id);
        Task DeleteDocument(int documentId);
        IQueryable<Document> Query();
        Task<List<DocumentFileExplorer>> GetFileExplorerAsync(int cabinetId);

        Task<List<string>> GetDocTypesAsync();
        Task<DocumentTypes> GetOrCreateDocLabelAsync(string label);
        Task<string> GetDocumentName(int id);
        void AddDocumentRange(Document doc);

        Task<int> GetDocumentIdFromPathAsync(string filePath); //need to check
        //--------NOTES----------------
        Task<List<NotesDto>> GetDocumentWithNotesAsync(int documentId);
        Task<Notes> AddNoteAsync(Notes note);
        void  UpdateNote(Notes note);
        void DeleteNote(Notes note);
        Task<Notes> GetNoteByIdAsync(long id);
        //----------------------- doc download link----------------

        Task CreateDocDownloadLinkAsync(IEnumerable<DocDownloadLink> entities);
        Task<List<DocDownloadGetDTO>> GetAllDocumentForDownload(int userid);

        Task<DocDownloadLink>GetByIdDownloadLinkAsync(int docid,int userid);
        Task<List<int>> GetActiveDocumentIdsForUserAsync(int userId, IEnumerable<int> documentIds);

        Task<int> CounterDocumentDownload(int docid, int userid);

    }
}
