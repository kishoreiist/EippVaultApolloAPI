using EVWebApi.Models;
namespace EVWebApi.Interfaces.Repositories
{
    public interface IMetadataRepository
    {
        Task AddMetadata(List<Metadata> metadata);
        Task<List<Metadata>> GetMetadataByDocumentId(int documentId);
        Task DeleteMetadataByDocumentId(int documentId);
    }
}
