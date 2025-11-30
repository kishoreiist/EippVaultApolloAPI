using EVWebApi.Data;
using EVWebApi.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace EVWebApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<AuditLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
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
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log and swallow errors for audit logging so it doesn't break main flow
                _logger?.LogError(ex, "Failed to save audit log: {Action} for User {UserId}", action, userId);
            }
        }


        //pagination+filter

        public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default
        )
        {
            // Bound page size to avoid expensive queries
            const int MaxPageSize = 1000;
            pageSize = Math.Min(Math.Max(1, pageSize), MaxPageSize);

            var query = _context.AuditLogs
                //.Include(a => a.UserId)
                .AsNoTracking()
                .AsQueryable();

            // FILTER: Username
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var usernameLower = userName.ToLower();
                query = query.Where(a => a.UserName != null && a.UserName.ToLower().Contains(usernameLower));
            }

            // FILTER: Module
            if (!string.IsNullOrWhiteSpace(module))
            {
                var moduleLower = module.ToLower();
                query = query.Where(a => a.Module != null && a.Module.ToLower() == moduleLower);
            }

            // FILTER: Action
            if (!string.IsNullOrWhiteSpace(action))
            {
                var actionLower = action.ToLower();
                query = query.Where(a => a.Action != null && a.Action.ToLower() == actionLower);
            }

            // FILTER: Date range
            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            // TOTAL COUNT BEFORE PAGINATION
            int totalCount = await query.CountAsync(cancellationToken);

            // APPLY PAGINATION + ORDERING
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (logs, totalCount);
        }

        // EXPORT TO CSV

        public async Task<byte[]> ExportLogsToCsvAsync(
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
            , CancellationToken cancellationToken = default
        )
        {
            // Stream pages to avoid loading massive datasets into memory at once
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,User,Module,Action,TargetId,Details,IP");

            int currentPage = 1;
            const int exportPageSize = 1000; // reasonable chunk size
            while (true)
            {
                var (logsChunk, _) = await GetLogsAsync(currentPage, exportPageSize, userName, module, action, fromDate, toDate, cancellationToken);
                if (logsChunk == null || !logsChunk.Any()) break;

                foreach (var log in logsChunk)
                {
                    sb.AppendLine(
                        $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                        $"{(log.UserId.HasValue ? log.UserId.Value.ToString() : string.Empty)}," +
                        $"{(string.IsNullOrEmpty(log.UserName) ? string.Empty : log.UserName.Replace("\"", "\"\""))}," +
                        $"{(string.IsNullOrEmpty(log.Module) ? string.Empty : log.Module.Replace("\"", "\"\""))}," +
                        $"{(string.IsNullOrEmpty(log.Action) ? string.Empty : log.Action.Replace("\"", "\"\""))}," +
                        $"{(log.TargetId.HasValue ? log.TargetId.Value.ToString() : string.Empty)}," +
                        $"\"{(string.IsNullOrEmpty(log.Details) ? string.Empty : log.Details.Replace("\"", "\"\""))}\"," +
                        $"{(string.IsNullOrEmpty(log.IpAddress) ? string.Empty : log.IpAddress)}"
                    );
                }

                if (logsChunk.Count() < exportPageSize) break;
                currentPage++;
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // Add explicit interface implementations for missing overloads

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
            // Call the main implementation, passing default CancellationToken
            return await GetLogsAsync(page, pageSize, userName, module, action, fromDate, toDate, default);
        }

        public async Task<byte[]> ExportLogsToCsvAsync(
            string? userName = null,
            string? module = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            // Call the main implementation, passing default CancellationToken
            return await ExportLogsToCsvAsync(userName, module, action, fromDate, toDate, default);
        }
    }
}
