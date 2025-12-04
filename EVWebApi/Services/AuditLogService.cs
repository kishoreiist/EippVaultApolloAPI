using AutoMapper;
using EVWebApi.Data;
using EVWebApi.DTOs.Audit;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Utilities;
using System.Text;
using System.Threading;


namespace EVWebApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuditLogService> _logger;

        private readonly IMapper _mapper;
        public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<AuditLogService> logger,IMapper mapper)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task LogAsync(int userId, string username,string module, string action, string? target = null, int? cabinetId = null,  string? details = null, string? filters = null)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(details))
            { 
                string? messageTemplate = _configuration.GetSection($"AuditMessages:CommonActions:{action}")?.Value
                                     ?? _configuration.GetSection("AuditMessages:Default:Message")?.Value;

            details = AuditMessageHelper.FormatMessage(
                messageTemplate,
                username,
                module,
                action,
                target,
                cabinetId,
                filters
            );
        }

            var log = new AuditLog
            {
                UserId = userId,
                UserName = username,
                Module = module,
                Action = action,
                Target = target,
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


        public async Task<PagedResponse<AuditLogDTO>> GetLogsAsync(AuditLogQueryParameters query,CancellationToken cancellationToken = default)
        { 


            var auditQuery = _context.AuditLogs.AsQueryable();

            // FILTERS

            if (query.FromDate.HasValue)
                auditQuery = auditQuery.Where(a => a.Timestamp >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                auditQuery = auditQuery.Where(a => a.Timestamp <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.search))
            {
                string term = query.search.ToLower();
                auditQuery = auditQuery.Where(a =>
                    (a.UserName != null && a.UserName.ToLower().Contains(term)) ||
                    (a.Module != null && a.Module.ToLower().Contains(term)) ||
                    (a.Action != null && a.Action.ToLower().Contains(term)) ||
                    (a.Details != null && a.Details.ToLower().Contains(term))
                );
            }
            // TOTAL COUNT BEFORE PAGINATION
            int totalCount = await auditQuery.CountAsync(cancellationToken);

            // APPLY PAGINATION + ORDERING
            var logs = await auditQuery
                .OrderByDescending(a => a.Timestamp)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var mapped = _mapper.Map<List<AuditLogDTO>>(logs);

            return new PagedResponse<AuditLogDTO>
            {
                Data = mapped,
                TotalRecords = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
            //return (logs, totalCount);
        }

        // EXPORT TO CSV

        public async Task ExportLogsToCsvAsync(
            int pagenumber,
            int pagesize,
            Stream outputStream,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default
        )
        {
            //var sb = new StringBuilder();
            //sb.AppendLine("Timestamp,User,Module,Action,TargetId,Details,IP");
            using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync("Timestamp,User,Module,Action,TargetId,Details,IP");

            //int pagenumber = 0;
            //const int pagesize = 10; // reasonable chunk size

            while (true)
            {

                var query = new AuditLogQueryParameters
                {
                    PageNumber = pagenumber,
                    PageSize = pagesize,
                    search = search,
                    FromDate = fromDate,
                    ToDate = toDate
                };
                var result = await GetLogsAsync(query);
                var logsChunk = result.Data;
                if (logsChunk == null || !logsChunk.Any())
                    break;
                //var (logsChunk, _) = await GetLogsAsync(offset, limit, userName, module, action, fromDate, toDate, cancellationToken);
                //if (logsChunk == null || !logsChunk.Any()) break;

                foreach (var log in logsChunk)
                {
                        await writer.WriteLineAsync(
                            $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                            $"{log.UserId}," + 
                            $"\"{(log.UserName ?? string.Empty).Replace("\"", "\"\"")}\"," + 
                            $"\"{(log.Module ?? string.Empty).Replace("\"", "\"\"")}\"," + 
                            $"\"{(log.Action ?? string.Empty).Replace("\"", "\"\"")}\"," + 
                            $"{(log.TargetId?.ToString() ?? string.Empty)}," +
                            $"\"{(log.Details ?? string.Empty).Replace("\"", "\"\"")}\"," +
                            $"{(log.IpAddress ?? string.Empty)}" 
                    );
                }

                if (logsChunk.Count() < pagesize) break;
                pagenumber += pagesize;
            }
            //return Encoding.UTF8.GetBytes(sb.ToString());
            await writer.FlushAsync();
        }


        // Add explicit interface implementations for missing overloads

        public async Task<PagedResponse<AuditLogDTO>> GetLogsAsync(AuditLogQueryParameters query)
        {
            // Call the main implementation, passing default CancellationToken
            return await GetLogsAsync(query,default);
        }

        public async Task ExportLogsToCsvAsync(
            int pagenumber,
            int pagesize,
            Stream outputStream,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            // Call the main implementation, passing default CancellationToken
             await ExportLogsToCsvAsync(pagenumber, pagesize,outputStream, search, fromDate, toDate, default);
        }


    }
}
