namespace EVWebApi.DTOs.Document
{
    public class DocumentResponseDto
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public List<MetadataDTO>? Metadata { get; set; }
    }

}
