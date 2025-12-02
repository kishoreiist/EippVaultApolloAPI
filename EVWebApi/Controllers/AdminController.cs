using EVWebApi.DTOs.Audit;
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
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryParameters query)
        {
            var result = await _auditLogService.GetLogsAsync(query);
            return Ok(result);
        }

        [HttpGet("logs/export")]
        public async Task ExportCsv(
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null
        )
        {
            Response.ContentType = "text/csv";
            Response.Headers.Add("Content-Disposition", "attachment; filename=audit_logs.csv");
            await _auditLogService.ExportLogsToCsvAsync(
                Response.Body,
                search,
                fromDate,
                toDate
            
            //stream.Position = 0;

            //return File(
            //    stream,
            //    "text/csv",
            //    "audit_logs.csv"
            );
        }

    }

}
