using EVWebApi.Models.HR;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IConfigurationRepository
    {
        Task<DocumentCollection?> GetCollectionByNameAsync(string name);
        Task<DocumentCollection?> GetCollectionByIdAsync(int id);
        IQueryable<DocumentCollection> Query();
        IQueryable<ConfigRequest> GetConfigListAsync();
        Task<ConfigRequestRecipient?> GetConfigRequestByToken(string token);
        Task<List<ConfigRequest>> GetConfigRequestAsync(string? status);
        Task<int?> GetUploadCount(int recipientId);
        Task<OnboardingDocument> GetOnboardingFilesAsync(int docid);
    }
}
