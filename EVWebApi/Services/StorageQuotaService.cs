using EVWebApi.Data;
using EVWebApi.DTOs.Plan;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Services;
using EVWebApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EVWebApi.Services
{
    public class StorageQuotaService : IStorageQuotaService
    {
        
        private readonly AppDbContext _context;
        public StorageQuotaService( AppDbContext context)
        {
            _context = context;
        }

        public async Task ValidateAndConsumeStorage(long incomingFileSize)
        {
            var plan = await _context.ClientPlanDetail
                .FirstOrDefaultAsync();
            if (plan == null)
                throw new Exception("Plan not found");


            if (plan.UsedSizeBytes + incomingFileSize > plan.PlanSizeBytes)
            {
                var remainingBytes = plan.PlanSizeBytes - plan.UsedSizeBytes;

                throw new LockedException(
                    $"Storage exceeded. Remaining: {FormatSize(remainingBytes)}"
                );
            }
            plan.UsedSizeBytes += incomingFileSize;
        }

        public async Task<PlanUsageDto> GetPlanUsageAsync()
        {
            var plan = await _context.ClientPlanDetail.FirstOrDefaultAsync();

            if (plan == null)
                throw new Exception("Plan not found");

            decimal usedGb = plan.UsedSizeBytes / (1024m * 1024m * 1024m);
            decimal totalGb = plan.PlanSizeBytes / (1024m * 1024m * 1024m);
            decimal remainingGb = totalGb - usedGb;
            decimal remainingBytes = plan.PlanSizeBytes - plan.UsedSizeBytes;

            return new PlanUsageDto
            {
                TotalGb = totalGb,
                UsedGb = usedGb,
                RemainingGb = remainingGb,
                UsagePercentage = (double)((usedGb / totalGb) * 100),
                RemaianingBytes= remainingBytes
            };
        }
        private string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024m * 1024m * 1024m):F2} GB";

            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024m * 1024m):F2} MB";

            return $"{bytes} bytes";
        }
    }
}
