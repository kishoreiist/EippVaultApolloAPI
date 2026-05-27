using EVWebApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class UserMfaToken {
    public int TokenId { get; set; }
    public int UserId { get; set; }
    [Column("mfa_token")]
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; }
}
