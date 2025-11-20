namespace EVWebApi.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string?> AuthenticateAsync(string email, string password);
        Task<string> GenerateJwtAfterMfaAsync(string email);
    }
}
