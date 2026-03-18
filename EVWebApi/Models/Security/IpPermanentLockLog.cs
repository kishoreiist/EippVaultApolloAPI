using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public class IpPermanentLockLog
    {
        [Column("id")]
        public int Id { get; set; }
        [MaxLength(45)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
