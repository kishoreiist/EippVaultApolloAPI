namespace EVWebApi.DTOs
{
    public class ForgotAccountDetailsDTO
    {
        public required  string Email { get; set; }
        public required string Action { get; set; }
        public string? RedirectUrl { get; set; }
    }
}