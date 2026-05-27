using AutoMapper;
using EVWebApi.Data;
using EVWebApi.DTOs.Audit;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Helpers;
using EVWebApi.Helpers.ExportToExcel;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Utilities;
using Syncfusion.XlsIO;
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

        public async Task LogAsync(int? userId, string username,string module, string action, string? target = null, int? cabinetId = null,  string? details = null, string? filters = null)
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


        public async Task<PagedResponse<AuditLogDTO>> GetLogsAsync(AuditLogQueryParameters query,int? userid,string usertype,CancellationToken cancellationToken = default)
        {


            IQueryable<AuditLog> auditQuery;

            if (usertype=="super_admin")//need to chekc the super_admin role
            {
                auditQuery = _context.AuditLogs.AsQueryable();
            }
            else
            {
               // For regular users, filter by their userId
                auditQuery = _context.AuditLogs.Where(a => a.UserId == userid);
            }

            // FILTERS


            if (query.FromDate.HasValue)
            {
                //var from = query.FromDate.Value.Date;
                auditQuery = auditQuery.Where(a => a.Timestamp >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                var to = query.ToDate.Value.Date.AddDays(1);
                auditQuery = auditQuery.Where(a => a.Timestamp < to);
            }
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
            int totalRecords = await auditQuery.CountAsync(cancellationToken);

            // If pageSize is invalid, normalize it
            if (query.PageSize <= 0)
                query.PageSize = 10;
            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalRecords / (double)query.PageSize);


            // Normalize pageNumber
            if (query.PageNumber <= 0)
                query.PageNumber = 1;

            if (query.PageNumber > totalPages && totalPages > 0)
                query.PageNumber = totalPages;
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
                TotalRecords = totalRecords,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

        }

        // EXPORT TO CSV

        public async Task ExportLogsToCsvAsync(
            int pagenumber,
            int pagesize,
            int? userid, string usertype,
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
            await writer.WriteLineAsync("Timestamp,User,Module,Action,Target,Details,IP");

            //int pagenumber = 0;
            //const int pagesize = 10; // reasonable chunk size

            int page = pagenumber; // start from query param
            while (true)
            {
                var query = new AuditLogQueryParameters
                {
                    PageNumber = page,
                    PageSize = pagesize,
                    search = search,
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var result = await GetLogsAsync(query, userid, usertype, cancellationToken);
                var logsChunk = result.Data;

                if (logsChunk == null || !logsChunk.Any())
                    break;

                foreach (var log in logsChunk)
                {
                    await writer.WriteLineAsync(
                        $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                        $"{log.UserId}," +
                        $"\"{(log.UserName ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(log.Module ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(log.Action ?? "").Replace("\"", "\"\"")}\"," +
                        $"{log.Target}," +
                        $"\"{(log.Details ?? "").Replace("\"", "\"\"")}\"," +
                        $"{log.IpAddress}"
                    );
                }

                await writer.FlushAsync(); // make sure data is written to stream

                if (logsChunk.Count() < pagesize)
                    break;

                page++; // increment page number by 1, not pagesize
            
        }
            //return Encoding.UTF8.GetBytes(sb.ToString());
            await writer.FlushAsync();
        }


        // Add explicit interface implementations for missing overloads

        public async Task<PagedResponse<AuditLogDTO>> GetLogsAsync(AuditLogQueryParameters query, int userid, string usertype)
        {
            // Call the main implementation, passing default CancellationToken
            return await GetLogsAsync(query,userid,usertype, default);
        }

        public async Task ExportLogsToCsvAsync(
            int pagenumber,
            int pagesize,
            int? userid, string usertype,
            Stream outputStream,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            // Call the main implementation, passing default CancellationToken
             await ExportLogsToCsvAsync(pagenumber, pagesize, userid, usertype,outputStream, search, fromDate, toDate, default);
        }

        // Aceslist &cabinetlist
        public async Task<PrivilegeConfigDto> GetPrivilegeConfigurationAsync()
        {
            return new PrivilegeConfigDto
            {

                AccessList = await _context.AccessRights
           .Select(x => new ListDto
           {
               Id = x.Id,
               Name = x.AccessName
           })
           .ToListAsync(),

                CabinetsList = await _context.Cabinets
           .Select(x => new ListDto
           {
               Id = x.CabinetId,
               Name = x.CabinetName
           })
           .ToListAsync()
            };
        }

        //export to excl

        public async Task<(byte[], string)> AuditLogsExportToExcel(
    AuditLogQueryParameters query, int? userId, string userType)
        {
            const int batchSize = 2000; // Industry-standard batch size
            int pageNumber = 1;
            query.PageSize = batchSize;

            var columns = new List<string>
            {
                "UserName", "Action", "IP Address", "Module", "Timestamp", "Message"
            };

            using var excelEngine = new ExcelEngine();
            IApplication app = excelEngine.Excel;
            app.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = app.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];
            sheet.Name = "AuditLogs";

            // Write header
            for (int c = 0; c < columns.Count; c++)
                sheet.Range[1, c + 1].Text = columns[c];

            sheet.Range[1, 1, 1, columns.Count].CellStyle.Font.Bold = true;

            int row = 2;

            while (true)
            {
                query.PageNumber = pageNumber;

                var pagedLogs = await GetLogsAsync(query, userId, userType);

                if (pagedLogs.Data == null || !pagedLogs.Data.Any())
                    break;
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                foreach (var log in pagedLogs.Data)
                {
                    for (int c = 0; c < columns.Count; c++)
                    {
                        var value = ExcelColumnsHelper.GetLogColumnValue(log, columns[c]);

                        //sheet.Range[row, c + 1].Text =
                        //    value is DateTime dt ? dt.ToString("dd/MM/yyyy HH:mm:ss") : value?.ToString();

                        if (value is DateTime dt)
                        {
                            var istTime = TimeZoneInfo.ConvertTimeFromUtc(dt, istZone);

                            sheet.Range[row, c + 1].Text =
                                istTime.ToString("dd/MM/yyyy HH:mm:ss");
                        }
                        else
                        {
                            sheet.Range[row, c + 1].Text = value?.ToString();
                        }
                    }
                    row++;
                }

                // If fewer rows than batch, we are done
                if (pagedLogs.Data.Count < batchSize)
                    break;

                pageNumber++;
            }

            sheet.UsedRange.AutofitColumns();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            string fileName = $"AuditLogs_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx";

            return (stream.ToArray(), fileName);
        }


    }
}
