using EVWebApi.DTOs;

namespace EVWebApi.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(string? username,string? email, string password);
        Task<string> GenerateJwtAfterMfaAsync(string email);
    }
}
