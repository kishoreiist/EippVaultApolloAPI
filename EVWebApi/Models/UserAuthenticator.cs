using EVWebApi.Models;

public class UserAuthenticator {
    public int AuthId { get; set; }
    public int UserId { get; set; }
    public string SecretKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; }
}
