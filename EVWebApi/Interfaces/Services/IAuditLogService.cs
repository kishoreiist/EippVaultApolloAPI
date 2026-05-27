using EVWebApi.DTOs.Audit;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;

namespace EVWebApi.Interfaces.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
                  int? userId,
                  string username,
                  string module,
                  string action,
                  string? target = null,
                  int? cabinetId = null,
                  string? details = null,
                  string? filters = null
              );

        Task<PagedResponse<DTOs.Audit.AuditLogDTO>> GetLogsAsync(AuditLogQueryParameters query,int? userid,string usertype, CancellationToken cancellationToken = default);
        //export to csv
        Task ExportLogsToCsvAsync(
            int pagenumber,
            int pagesize,
            int? userid, string usertype,
            Stream outputStream,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
        Task<(byte[], string)> AuditLogsExportToExcel(AuditLogQueryParameters query, int? userId, string userType);
        Task<PrivilegeConfigDto> GetPrivilegeConfigurationAsync();
    }

}
