using EVWebApi.Models;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocVersionService
    {
        Task<List<DocumentVersion>> GetDocumentVersionsAsync(int id);
        Task<DocumentVersion> CreateVersionAsync(DocumentVersion dto, int VersionNo);

        Task<DocumentLock> CreateDocumentLockAsync(int docId, int? userId);
        Task<DocumentLock> CheckDocLockValidityAsync(int docId, int? userId);
        Task ReleaseLockAsync(int docId, int? userId);
    }
}
