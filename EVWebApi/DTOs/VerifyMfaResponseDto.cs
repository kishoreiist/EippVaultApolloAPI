namespace EVWebApi.DTOs
{
    public class VerifyMfaResponseDto
    {

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public Guid SessionId { get; set; }

    }

    
    public class RefreshResultDTO
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

}
