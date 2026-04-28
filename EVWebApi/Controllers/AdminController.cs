using Azure;
using EVWebApi.DTOs.Audit;
using EVWebApi.DTOs.HR;
using EVWebApi.DTOs.User;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using EVWebApi.Services;
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
    public class AdminController : BaseController
    {
        private readonly IAuditLogService _auditLogService;
        private readonly NpgsqlDataSource _dataSource;
        public AdminController(IAuditLogService auditLogService, NpgsqlDataSource dataSource)
        {
            _auditLogService = auditLogService;
            _dataSource = dataSource;
        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryParameters query)
        {
            var result = await _auditLogService.GetLogsAsync(query, 43, "super_admin");
            return Ok(result);
        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("privileges")]
        public async Task<IActionResult> GetPrivilegeConfiguration()
        {
            var result = await _auditLogService.GetPrivilegeConfigurationAsync();
            return Ok(result);
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("export_csv")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] int pagenumber = 1,
            [FromQuery] int pagesize = 100,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null
        )
        {
            Response.ContentType = "text/csv";
            Response.Headers.Add("Content-Disposition", "attachment; filename=audit_logs.csv");
            await _auditLogService.ExportLogsToCsvAsync(
                pagenumber,
                pagesize, CurrentUserId, CurrentUserType,
                Response.Body,
                search,
                fromDate,
                toDate

            );
            return new EmptyResult();
        }


        [Authorize(Roles = "admin,super_admin")]
        [HttpPost("export_excel")]
        public async Task<IActionResult> ExportLogs([FromQuery] AuditLogQueryParameters query)
        {
            var (bytes, fileName) = await _auditLogService.AuditLogsExportToExcel(query, CurrentUserId, CurrentUserType);
            await _auditLogService.LogAsync(CurrentUserId, CurrentUsername, "Audit Logs", "Export Excel");
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(DateTime? start_date, DateTime? end_date)
        {
            try
            {
                var user_Id = int.Parse(User.FindFirst("userId")?.Value
                               ?? throw new Exception("UserId not found in token"));

                if (start_date == null || end_date == null)
                {
                    var range = PreviousWeekDateHelper.GetPreviousWeekRange();
                    start_date = range.start;
                    end_date = range.end;
                }

                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = new NpgsqlCommand("SELECT get_dashboard_data(@p_start_date, @p_end_date,@p_user_id)", conn);
                cmd.Parameters.AddWithValue("p_start_date", NpgsqlTypes.NpgsqlDbType.Date, start_date);
                cmd.Parameters.AddWithValue("p_end_date", NpgsqlTypes.NpgsqlDbType.Date, end_date);
                cmd.Parameters.AddWithValue("p_user_id", user_Id);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return Ok(new { });

                var json = result.ToString();
                var rawData = JsonSerializer.Deserialize<DashboardDTO>(json);


                var uploadPercentage = new
                {
                    labels = rawData.upload_percentage.Select(x => x.day).ToList(),
                    //counts = rawData.upload_percentage.Select(x => x.count).ToList(),
                    data = rawData.upload_percentage.Select(x => x.percentage).ToList()
                };

                var cabinetDistribution = new
                {
                    labels = rawData.cabinet_distribution.Select(x => x.cabinet).ToList(),
                    data = rawData.cabinet_distribution.Select(x => x.percentage).ToList()
                };
                var userActivityPercentage = new
                {
                    labels = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                    data = rawData.user_activity_percentage
                };

                var response = new
                {
                    kpis = rawData.kpis,
                    uploadPercentage,
                    cabinetDistribution,
                    userActivityPercentage,
                    recent_document_activity = rawData.recent_document_activity
                };

                return Ok(response);

            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching dashboard data.");

            }


        }
        [HttpGet("hr_dashboard")]
        public async Task<IActionResult> GetHRDashboard([FromQuery] HrDashboardQueryParameters query)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();

                string sql;
                var cmd = new NpgsqlCommand();
                cmd.Connection = conn;


                if (!string.IsNullOrWhiteSpace(query.Region))
                {
                    sql = "SELECT public.fn_hr_dashboard_region_login(@p_region)";
                    cmd.Parameters.AddWithValue("p_region", NpgsqlTypes.NpgsqlDbType.Text, query.Region);
                }
                else
                {
                    sql = "SELECT public.fn_hr_dashboard_final_correct()";
                }

                cmd.CommandText = sql;

                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    return Ok(new { });

                var json = result.ToString();
                var parsed = JsonDocument.Parse(json!);

                return Ok(parsed);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching dashboard data.");

            }
        }

        [HttpGet("po_dashboard")]
        public async Task<IActionResult> GetPODashboard([FromQuery] PoDashboardQueryParameters query)
        {
            try
            {
                await using var conn = await _dataSource.OpenConnectionAsync();

                var cmd = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = "SELECT public.get_po_dashboard(@p_user_id, @p_page, @p_page_size)"
                };

                var userId = CurrentUserId;
                cmd.Parameters.AddWithValue("p_user_id", NpgsqlTypes.NpgsqlDbType.Integer, userId);
                cmd.Parameters.AddWithValue("p_page", NpgsqlTypes.NpgsqlDbType.Integer, query.PageNumber);
                cmd.Parameters.AddWithValue("p_page_size", NpgsqlTypes.NpgsqlDbType.Integer, query.PageSize);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    return Ok(new { });

                var json = result.ToString();

                var parsed = JsonDocument.Parse(json!);

                return Ok(parsed);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching dashboard data.");
            }
        }
    }
}
