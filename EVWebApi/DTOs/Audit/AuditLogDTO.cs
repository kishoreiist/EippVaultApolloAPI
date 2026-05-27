using EVWebApi.DTOs.Pagination;

namespace EVWebApi.DTOs.Audit
{
    public class AuditLogDTO
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string Module { get; set; }
        public string? UserName { get; set; }
        public string? Target { get; set; }
        public string? Details { get; set; }
    }
}
