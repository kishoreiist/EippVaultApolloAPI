namespace EVWebApi.DTOs
{
    public class SendEmailRequest
    {

        public string ToEmail { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
        public List<string> SelectedInvoiceIds { get; set; } = new();

    }
}
