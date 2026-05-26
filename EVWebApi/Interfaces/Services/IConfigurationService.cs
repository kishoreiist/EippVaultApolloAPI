using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.HR;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Models;
using EVWebApi.Models.HR;

namespace EVWebApi.Interfaces.Services
{
    public interface IConfigurationService
    {
        Task<CollectionResponseDto> CreateCollectionAsync(CreateCollectionDto dto, int? userId);
        Task<CollectionResponseDto> UpdateCollectionAsync(int id, CreateCollectionDto dto, int? userId);
        Task<List<CollectionDropDownDto>> GetCollectionDropDownListAsync(CollectionDropDownQueryDto dto);
        Task<CollectionResponseDto> GetCollectionByIdAsync(int id);
        Task<PagedResponse<CollectionListResponseDto>> GetCollectionListAsync(CollectionQueryDto dto);
        Task DeleteCollectionAsync(int id);
        Task<UploadPageResponseDto> GetUploadDocsAsync(string token);
        Task<ConfigurationResponseDto> SendConfigurationAsync(ConfigurationRequestDto dto, int userId);
       // Task<UploadResultDto> UploadDocumentsAsync(OnboardingDocsDto dto);
        Task<UploadResultDto> MainUploadDocumentsAsync(OnboardingDocsDto dto);
        Task<List<ConfigRequestDetailsDto>> GetConfigRequestsAsync(ConfigQueryDetailDto dto);
        Task<DocumentStreamResultDTO?> GetOnboardingDocumentStream(int id);

        Task<HrUploadResponseDto> OnboardingExcelUploadAsync(OnboardingUploadDto dto,int? userId);
        Task<(byte[], string)> ExportFailedRowsAsync(int batchId);
        Task<ConfirmedCandidateDto> ConfirmOnboardingBatchAsync(int batchId, int userId);
        Task<Document> ConfirmCandidateAsync(ConfirmIndivitualCandidateDto dto, int userId);

        Task<DocumentResponseDto> SplitOnboardingDocumentAsync(SplitAndExtractPdfDto dto);
        Task<(byte[], string)> ExportOnboardingReport(ExportOnboardingReportQuery query);

        Task<OpenExcelDto> GetOnboardingExcelSheetNamesAsync(DocumentRequestDto dto);
        Task<string> OpenOnboardingExcelSheetAsync(DocumentExcelOpenDTO dto);
        Task<StatusCountResponseDto> GetCandidatesStatusCountAsync(StatusCountQueryParamDto dto);
        Task<bool> SendLaptopRequestMailAsync(RequestLaptopDto dto, CancellationToken ct = default);
        Task<string> RemoveCandidateAsync(int candidatid,int userid);
        Task<List<CompletedRecipientDto?>> GetRecipientDetailsAsync(int candidateId);
        Task<BatchResponseDTO> ApplyOboardingExcelPatchAsync(ExcelPatchRequestDto dto, int? userId);

    }
}
