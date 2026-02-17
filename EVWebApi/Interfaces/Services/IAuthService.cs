using EVWebApi.DTOs;
using EVWebApi.Models;

namespace EVWebApi.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(LoginRequestDTO loginRequest);
        Task<AuthResult> AuthenticateAsync(string? username,string? email, string password);
        Task<string> GenerateJwtAfterMfaAsync(string email);
        string GeneratePasswordResetJwtAsync(User user);
        Task PasswordResetAsync(string token, string password);


        Task<bool> PasswordResetSendEmailAsync(User user);


    }
}
