using EVWebApi.Models;
namespace EVWebApi.Interfaces.Repositories
{
    public interface IMetadataRepository
    {
        Task AddMetadata(IEnumerable<Metadata> metadata);
        Task<List<Metadata>> GetMetadataByDocumentId(int documentId);
        Task DeleteMetadataByDocumentId(int documentId);
    }
}
