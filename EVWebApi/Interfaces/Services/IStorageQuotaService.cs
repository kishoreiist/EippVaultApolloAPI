using EVWebApi.DTOs.Plan;

namespace EVWebApi.Interfaces.Services
{
    public interface IStorageQuotaService
    {
        //Task EnsureStorageAvailable(long incomingFileSize);
        Task<PlanUsageDto> GetPlanUsageAsync();
        Task ValidateAndConsumeStorage(long incomingFileSize);
    }
}
