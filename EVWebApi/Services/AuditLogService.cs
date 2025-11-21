using EVWebApi.Data;
using EVWebApi.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace EVWebApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task LogAsync(int userId, string module, string action, int? targetId = null, string? details = null)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(details))
            {
                string? messageTemplate = null;
                messageTemplate = _configuration.GetSection($"AuditMessages:CommonActions:{action}")?.Value;
                if (string.IsNullOrEmpty(messageTemplate))
                {
                    messageTemplate = _configuration.GetSection("AuditMessages:Default:Message").Value;
                }
                details = messageTemplate?
            .Replace("{targetId}", targetId?.ToString() ?? "N/A")
            .Replace("{module}", module)
            .Replace("{action}", action);
            }

            var log = new AuditLog
            {
                UserId = userId,
                Module = module,
                Action = action,
                TargetId = targetId,
                Details = details,
                Timestamp= DateTime.UtcNow,
                IpAddress = ip
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }


        //pagination+filter

        public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {


            var query = _context.AuditLogs
                //.Include(a => a.UserId)
                .AsQueryable();

            // FILTER: Username
            if (!string.IsNullOrWhiteSpace(userName))
            {
                query = query.Where(a =>
                    a.UserId != null &&
                    a.UserName.ToLower().Contains(userName.ToLower()));
            }

            // FILTER: Module
            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(a => a.Module.ToLower() == module.ToLower());
            }

            // FILTER: Action
            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action.ToLower() == action.ToLower());
            }

            // FILTER: Date range
            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            // TOTAL COUNT BEFORE PAGINATION
            int totalCount = await query.CountAsync();

            // APPLY PAGINATION + ORDERING
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        // EXPORT TO CSV

        public async Task<byte[]> ExportLogsToCsvAsync(
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            var (logs, _) = await GetLogsAsync(
                1, int.MaxValue, userName, module, action, fromDate, toDate);

            var sb = new StringBuilder();

            // CSV Header
            sb.AppendLine("Timestamp,User,Module,Action,TargetId,Details,IP");

            foreach (var log in logs)
            {
                sb.AppendLine(
                    $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                    $"{log.UserId}," +
                    $"{log.UserName}," +
                    $"{log.Module}," +
                    $"{log.Action}," +
                    $"{log.TargetId}," +
                    $"\"{log.Details}\"," +
                    $"{log.IpAddress}"
                );
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
