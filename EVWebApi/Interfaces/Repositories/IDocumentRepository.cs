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

        //--------NOTES----------------
        Task<List<NotesDto>> GetDocumentWithNotesAsync(int documentId);
        Task<Notes> AddNoteAsync(Notes note);
        void  UpdateNote(Notes note);
        void DeleteNote(Notes note);
        Task<Notes> GetNoteByIdAsync(long id);

    }
}
