public class AuditLog {

    public int Id { get; set; }
    public int LogId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; }
    public bool MfaAttempt { get; set; }
    public string MfaStatus { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
}
