using EVWebApi.Models;
using System.Security.Cryptography;

namespace EVWebApi.Helpers
{
    public class SecurityHelper
    {
        //to generate otp
        public static string GenerateSecureOtp()
        {
            // Generates a number between 000000 and 999999
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);

            var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return value.ToString("D6");
        }



    }
}


