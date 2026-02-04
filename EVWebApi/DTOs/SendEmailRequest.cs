


namespace EVWebApi.DTOs
{
    public class SendEmailRequest
    {
        public string To { get; set; }
        public List<string>? Cc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string>? AttachmentPaths { get; set; }
    }

    public class LinkEmailDto
    {
        public string ReplyTo { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? To { get; set; }
        public List<string>? Cc { get; set; }
        public string Subject { get; set; }
        public string? Body { get; set; }
        public List<int> AttachmentIds { get; set; }
        public int MaxDownloads { get; set; } = 2;

       
    }
}
