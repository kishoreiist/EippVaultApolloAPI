namespace EVWebApi.Interfaces.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
                  int userId,
                  string module,
                  string action,
                  int? targetId = null,
                  string? details = null
              );

        //pagination+filter 

        Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
        //export to csv
        Task<byte[]> ExportLogsToCsvAsync(
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
    }

}
