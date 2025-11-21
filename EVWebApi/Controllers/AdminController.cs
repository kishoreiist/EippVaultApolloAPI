using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AdminController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }


        [HttpGet("logs")]
        public async Task<IActionResult> GetAuditLogs(
            int page = 1,
            int pageSize = 20,
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            var (logs, totalCount) = await _auditLogService.GetLogsAsync(
                page,
                pageSize,
                userName,
                module,
                action,
                fromDate,
                toDate
            );

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                data = logs
            });
        }


        [HttpGet("logs/export")]
        public async Task<IActionResult> ExportCsv(
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            var csvBytes = await _auditLogService.ExportLogsToCsvAsync(
                userName,
                module,
                action,
                fromDate,
                toDate
            );

            return File(
                csvBytes,
                "text/csv",
                "audit_logs.csv"
            );
        }
    }
}
