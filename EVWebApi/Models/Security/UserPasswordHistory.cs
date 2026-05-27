using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public class UserPasswordHistory
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("password_hash")]
        public required string PasswordHash { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
