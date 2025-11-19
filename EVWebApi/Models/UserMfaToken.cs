using EVWebApi.Models;

public class UserMfaToken {
    public int TokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; }
}
