namespace EVWebApi.DTOs.HR
{
    public class AccessRequestDto
    {
        public int DocLinkId { get; set; }

        public string? Reason { get; set; }
    }

    public class AccessActionDto
    {
        public int RequestId { get; set; }

        public string Action { get; set; } = null!; // "approve" | "reject"

        public int? MaxDownload { get; set; } // required for approve

        public string? Reason { get; set; }   // required for reject
    }
    public class RequestLaptopDto
    {
        public required List<int> CandidateIds { get; set; }
        public required string To { get; set; }
        public List<string>? Cc { get; set; }
        public string Message { get; set; }
        public required string Subject { get; set; }

    }

}
