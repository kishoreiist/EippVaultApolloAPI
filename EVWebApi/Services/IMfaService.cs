using EVWebApi.Models;

namespace EVWebApi.Services
{
    public interface IMfaService
    {
        Task<bool> VerifyTokenAsync(string email, string token);
        //Task StartMfaAsync(string email);
        Task GenerateAndSendTokenAsync(User user);
    }
}
