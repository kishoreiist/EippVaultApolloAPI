using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IDocVersionRepository: IGenericRepository<DocumentVersion>
    {
        Task<DocumentVersion> GetLatestVersionAsync(int id);
        Task<List<DocumentVersion>> GetVersionsAsync(int documentId);
        Task<DocumentLock?> GetLockByDocumentIdAsync(int documentId);
        void RemoveLock(DocumentLock entity);
        Task AddLockAsync(DocumentLock entity);
        Task<List<DocumentVersion>> GetArchivedVersions(int docId);
        Task GetOldVersionsToDelete(int docId, int keepCount);
        Task DeleteOldArchivedVersions(int docId);

    }
}
