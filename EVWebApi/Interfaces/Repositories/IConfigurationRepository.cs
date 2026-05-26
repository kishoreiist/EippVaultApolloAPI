using EVWebApi.DTOs.HR;
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
        Task<List<ConfigRequest>> GetConfigRequestAsync(ConfigQueryDetailDto dto);
        Task<int?> GetUploadCount(int recipientId, int candidateId);
        Task<OnboardingDocument> GetOnboardingFilesAsync(int docid);
        Task<ConfigRequestRecipient?> MatchOnboardingCandidateAsync(HrParsedRowDto row);
        Task<HrConfirmationBatch> CreateOnboardingBatch(HrConfirmationBatch batch);
        Task<HrConfirmationBatchRow> CreateOnboardingBatchRows(HrConfirmationBatchRow batchrows);
        Task<string> GetOnboardingFileNameById(int id);
        Task<List<int>> GetActiveOnboardDocIdsForUserAsync(int userId, IEnumerable<int> documentIds);
       Task<OnboardingDocument> DeleteOnboardingDocument(int id);

        IQueryable<Candidate> GetCandidateDocsById(int candidateId);
        Task<List<int>> GetExisitngCandidatesByEmail(List<string> emails);
        Task<Candidate> GetCandidateByIdAsync(int id);
        Task<ConfigRequestRecipient?> GetRecipientReqByCandidateId(int candidateId);
    }
}
