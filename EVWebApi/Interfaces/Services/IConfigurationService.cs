using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.HR;
using EVWebApi.DTOs.Pagination;

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
        Task<UploadResultDto> UploadDocumentsAsync(OnboardingDocsDto dto);
        Task<List<ConfigListDto>> GetAllConfigsAsync(int userId, string userType, ConfigQueryParamsDto dto);
        Task<List<ConfigRequestDetailsDto>> GetConfigRequestsAsync( ConfigQueryDetailDto dto);
        Task<DocumentStreamResultDTO?> GetDocumentStream(int id);
    }
}
