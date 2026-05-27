using EVWebApi.Interfaces.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace EVWebApi.Helpers
{
    public class RefreshTokenHelper
    {

        public static string HashToken(string token)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(token);
            byte[] hashBytes = SHA256.HashData(inputBytes); // Faster, no object allocation
            return Convert.ToBase64String(hashBytes);
        }
        public static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public static async Task<int?> GetUserIdFromRefreshToken(HttpContext context, string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) return null;

            // Resolve session repository from DI
            var sessionRepo = context.RequestServices.GetRequiredService<IUserSessionRepository>();

            var hashedToken = HashToken(refreshToken);

            // Lookup session in DB
            var session = await sessionRepo.GetByRefreshTokenHashAsync(hashedToken); 
            return session?.UserId;
        }
    }
}
