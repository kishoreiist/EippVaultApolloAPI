namespace EVWebAPI.Models
{
    public class MfaVerifyRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }
}