namespace EVWebApi.DTOs
{
    public class VerifyMfaRequest
    {

        public string Email { get; set; }
        public string Code { get; set; }
        public string Token { get; set; }
        public string Method { get; set; }

    }
}
