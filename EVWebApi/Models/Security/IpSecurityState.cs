using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public enum IpSecurityStatus
    {
        Normal = 0,
        Warning = 1,
        TempBlacklisted = 2,
        Blacklisted=3
    }

    public class IpSecurityState
    {
        [Column("id")]
        public int Id { get; set; }
        [MaxLength(45)]
        [Column("ip_address")]
        public required string IpAddress { get; set; }

        [Column("status")]
        public IpSecurityStatus Status { get; set; } = IpSecurityStatus.Normal;
        [Column("blacklisted_at")]
        public DateTime? BlacklistedAt { get; set; }
        [Column("last_activity_at")]
        public DateTime LastActivityAt { get; set; }


        [Column("ip_daily_failures")]
        public int IPDailyFailures { get; set; } = 0;
        [Column("ip_weekly_failures")]
        public int IPWeeklyFailures { get; set; } = 0;
        [Column("valid_upto")]
        public DateTime? ValidUpto { get; set; }
    }


}

        

    


    
