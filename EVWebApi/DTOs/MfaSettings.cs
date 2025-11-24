namespace EVWebApi.DTOs
{
    public class MfaSettings
    {
        public string Issuer { get; set; } = "MyApp";
        public string EmailSubject { get; set; } = "Your verification code";
        public string EmailBodyTemplate { get; set; } =
            "Your verification code is <b>{CODE}</b>. It expires in {MINUTES} minutes.";
    }
}
