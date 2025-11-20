using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document> CreateDocument(Document doc);
        //Task DeleteDocument(int id);
        Task<Document> GetDocument(int id);
        Task<int> GetLatestVersion(int cabinetId, string fileName);
        Task UpdateStatus(int id, string status);
        Task DeleteDocument(int documentId);

    }
}
