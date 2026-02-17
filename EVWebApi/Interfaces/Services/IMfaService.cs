using EVWebApi.Models;

namespace EVWebApi.Interfaces.Services
{
    public interface IMfaService
    {
        Task<bool> VerifyTokenAsync(string email, string token);
        //Task StartMfaAsync(string email);
        Task<bool> GenerateAndSendTokenAsync(User user);
        Task<bool> VerifyEmailOtpAsync(string email, string token);

        // GOOGLE TOTP
        Task<string> GenerateQrCodeAsync(int userId, string email);  // returns base64 PNG (no prefix)
        Task<bool> VerifyTotpAsync(int userId, string code);


    }
}
