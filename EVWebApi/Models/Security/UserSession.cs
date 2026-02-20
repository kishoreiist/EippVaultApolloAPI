using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public class UserSession
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("session_id")]
        public Guid SessionId { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("jwt_id")]
        public Guid JwtId { get; set; } = default!;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }
        //[Column("is_revoked")]
        [NotMapped]
        public bool IsRevoked => RevokedAt != null;

        [Column("ip_address")]
        public string? IpAddress { get; set; }
        [Column("device_info")]
        public string? DeviceInfo { get; set; }
        [Column("refresh_token_hash")]
        public string RefreshTokenHash { get; set; }
        [Column("last_activity_at")]
        public DateTime LastActivityAt { get; set; }

    }
}
