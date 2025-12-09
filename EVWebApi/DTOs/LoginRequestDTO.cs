namespace EVWebApi.DTOs
{
    public class LoginRequestDTO
    {
        public string? Email { get; init; }
        public string? Username { get; init; }
        public required string Password { get; init; }
        public string? TwoFactorCode { get; init; }
        public string? TwoFactorRecoveryCode { get; init; }
    }
}

