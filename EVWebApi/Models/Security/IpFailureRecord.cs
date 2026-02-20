using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{

    public class IpFailureRecord
     {
        [Column("id")]
        public int Id { get; set; }
        [MaxLength(45)]
        [Column("ip_address")]
        public required string IpAddress { get; set; }
        [Column("daily_failures")]
        public int DailyFailures { get; set; } = 0;
        [Column("weekly_failures")]
        public int WeeklyFailures { get; set; } = 0;
        [Column("last_failed_at")]
        public DateTime LastFailedAt { get; set; }
        [Column("valid_upto")]
        public DateTime? ValidUpto { get; set; }
    }

    
}
