namespace EVWebApi.DTOs
{
    public sealed class EmailSettings
    {

        public string Provider { get; set; } = "Smtp";
        public string From { get; set; } = default!;
        public string? DisplayName { get; set; }
        public SmtpSettings Smtp { get; set; } = new();

    }

}
