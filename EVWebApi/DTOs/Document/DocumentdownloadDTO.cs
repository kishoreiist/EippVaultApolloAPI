namespace EVWebApi.DTOs.Document
{
    public class DocumentDownloadDto
    {
        public string FilePath { get; set; } = string.Empty;  // full path to file
        public string FileName { get; set; } = string.Empty;
    }
    public class DocDownloadGetDTO
    {
        public int DocumentLinkId { get; set; }
        public int? DocumentId { get; set; }
        public int? OnboardingDocId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int RemainingDownloads { get; set; }  
        public string FileName { get; set; } 
    }
}
