namespace EVWebApi.DTOs
{
    public class AuthResult
    {
        public bool MfaRequired { get; set; }
        public string? Token { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
    }
}
