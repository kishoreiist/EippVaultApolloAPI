using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class AuditLog {

    //public int Id { get; set; }
    [Key]
    [Column("log_id")]
    public int LogId { get; set; }
    [Column("user_id")]
    public int? UserId { get; set; }
    [Column("action")]
    public string Action { get; set; }
    [Column("mfa_attempt")]
    public bool MfaAttempt { get; set; }
    [Column("mfa_status")]
    public string? MfaStatus { get; set; }
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }
    [Column("ip_address")]
    public string? IpAddress { get; set; }



    [Column("module")]
    public string? Module { get; set; }
    [Column("username")]
    public string? UserName { get; set; }
    [Column("target_id")]
    public int? TargetId { get; set; }
    [Column("details")]
    public string? Details { get; set; }
}
