//namespace EVWebApi.DTOs
//{
//    public class SendEmailRequest
//    {

//        public string ToEmail { get; set; } = default!;
//        public string Subject { get; set; } = default!;
//        public string Body { get; set; } = default!;
//        public List<string> SelectedInvoiceIds { get; set; } = new();

//    }
//}


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
}
