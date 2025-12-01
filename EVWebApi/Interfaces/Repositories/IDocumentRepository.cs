using EVWebApi.Models;

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

    }
}
