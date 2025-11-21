namespace EVWebApi.DTOs
{
    public class EnableMfaRequest
    {
        public string Email { get; set; }
        public string Method { get; set; }
        public string Issuer { get; set; } // Optional, only needed for GOOGLE MFA
    }
}
