
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class Notification
    {
        [Column("notification_id")]
        public int NotificationId { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("status")]
        public string Status { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
