namespace EVWebApi.DTOs
{
    public class ForgotPassword
    {
        public string Email { get; set; }
        public string? RedirectUrl { get; set; }
    }
}