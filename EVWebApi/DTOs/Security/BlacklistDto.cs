using EVWebApi.DTOs.Pagination;
using EVWebApi.Models.Security;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Security
{
    public class BlacklistDto
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public IpSecurityStatus Status { get; set; }
        public int IPDailyFailures { get; set; }
        public int IPWeeklyFailures { get; set; }
        public DateTime? BlacklistedAt { get; set; }
        public DateTime? ValidUpto { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public string UnlockedBy { get; set; }
    }

    public class BlacklistQueryParameters: QueryParameters
    {
        [FromQuery(Name ="ip_address")]
        public string? IpAddress { get; set; }
        [FromQuery(Name = "status")]
        public IpSecurityStatus? Status { get; set; }
        [FromQuery(Name = "blacklisted_from")]
        public DateTime? BlacklistedFrom { get; set; }
        [FromQuery(Name = "blacklisted_to")]
        public DateTime? BlacklistedTo { get; set; }
        [FromQuery(Name = "last_activity_from")]
        public DateTime? LastActivityFrom { get; set; }
        [FromQuery(Name = "last_activity_to")]
        public DateTime? LastActivityTo { get; set; }
    }
}
