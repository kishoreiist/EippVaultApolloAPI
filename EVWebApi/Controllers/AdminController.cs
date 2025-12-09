using EVWebApi.DTOs.Audit;
using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace EVWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly NpgsqlDataSource _dataSource;
        public AdminController(IAuditLogService auditLogService, NpgsqlDataSource dataSource)
        {
            _auditLogService = auditLogService;
            _dataSource = dataSource;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryParameters query)
        {
            var result = await _auditLogService.GetLogsAsync(query);
            return Ok(result);
        }

        [HttpGet("logs/export")]
        public async Task ExportCsv(
            [FromQuery] int pagenumber,
            [FromQuery] int pagesize,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null
        )
        {
            Response.ContentType = "text/csv";
            Response.Headers.Add("Content-Disposition", "attachment; filename=audit_logs.csv");
            await _auditLogService.ExportLogsToCsvAsync(
                pagenumber,
                pagesize,
                Response.Body,
                search,
                fromDate,
                toDate
            );
        }



        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(DateTime startDate, DateTime endDate,int userId)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = new NpgsqlCommand("SELECT get_dashboard_data(@p_start_date, @p_end_date,@p_user_id)", conn);
                cmd.Parameters.AddWithValue("p_start_date", NpgsqlTypes.NpgsqlDbType.Date, startDate);
                cmd.Parameters.AddWithValue("p_end_date", NpgsqlTypes.NpgsqlDbType.Date, endDate);
                cmd.Parameters.AddWithValue("p_user_id", userId);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return Ok(new {});

                var json = result.ToString();
                var rawData = JsonSerializer.Deserialize<DashboardDTO>(json);


                var uploadPercentage = new
                {
                    labels = rawData.upload_percentage.Select(x => x.day).ToList(),
                    counts = rawData.upload_percentage.Select(x => x.count).ToList(),
                    percentages = rawData.upload_percentage.Select(x => x.percentage).ToList()
                };

                var cabinetDistribution = new
                {
                    labels = rawData.cabinet_distribution.Select(x => x.cabinet).ToList(),
                    data = rawData.cabinet_distribution.Select(x => x.percentage).ToList()
                };


                var response = new
                {
                    kpis = rawData.kpis,
                    uploadPercentage,
                    cabinetDistribution,
                    userActivityPercentage = rawData.user_activity_percentage
                };

                return Ok(response);

            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching dashboard data.");

            }


        }
    }
}
