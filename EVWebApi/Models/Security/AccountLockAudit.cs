using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public class AccountLockAudit
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }


        [MaxLength(20)]
        [Column("lock_type")]
        public required string LockType { get; set; } // TEMP / PERMANENT
        [Column("reason")]
        public string? Reason { get; set; }
        [Column("locked_at")]
        public DateTime LockedAt { get; set; }
        [Column("locked_until")]

        public DateTime? LockedUntil { get; set; }
        [Column("unlocked_at")]
        public DateTime? UnlockedAt { get; set; }
      
        [MaxLength(50)]
        [Column("unlocked_by")]
        public string? UnlockedBy { get; set; }

        // Navigation (optional)
        public User User { get; set; }
    }
}
