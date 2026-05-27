using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public class UserFailureRecord
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [MaxLength(100)]
        [Column("endpoint")]
        public required string Endpoint { get; set; }

        [MaxLength(45)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }
        [Column("daily_failures")]
        public int DailyFailures { get; set; } = 0;
        [Column("weekly_failures")]
        public int WeeklyFailures { get; set; } = 0;

        [Column("last_failed_at")]
        public DateTime LastFailedAt { get; set; }


        [Column("valid_upto")]
        public DateTime? ValidUpto { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }


        // Navigation
        public User User { get; set; }
    }
}
