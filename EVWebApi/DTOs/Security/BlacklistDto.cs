using EVWebApi.DTOs.Pagination;
using EVWebApi.Models.Security;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Security
{
    public class BlacklistDto
    {
        public string IpAddress { get; set; }
        public IpSecurityStatus Status { get; set; }
        public int DailyFailures { get; set; }
        public int WeeklyFailures { get; set; }
        public DateTime? BlacklistedAt { get; set; }
        public DateTime? ValidUpto { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }

    public class BlacklistQueryParameters: QueryParameters
    {
        [FromQuery(Name ="ip_address")]
        public string? IpAddress { get; set; }
        [FromQuery(Name = "status")]
        public IpSecurityStatus? Status { get; set; }
        [FromQuery(Name = "blacklisted_from")]
        public DateTimeOffset? BlacklistedFrom { get; set; }
        [FromQuery(Name = "blacklisted_to")]
        public DateTimeOffset? BlacklistedTo { get; set; }
        [FromQuery(Name = "last_activity_from")]
        public DateTimeOffset? LastActivityFrom { get; set; }
        [FromQuery(Name = "last_activity_to")]
        public DateTimeOffset? LastActivityTo { get; set; }
    }
}
