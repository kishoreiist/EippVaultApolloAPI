using System.Text.Json.Serialization;

namespace EVWebAPI.Models
{
    public class MfaVerifyRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string Method { get; set; }
        [JsonIgnore]
        public string Code => Token;


    }
}