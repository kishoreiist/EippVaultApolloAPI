namespace EVWebApi.DTOs.Document
{
    public class BatchUploadResponseDTO
    {
        public int TotalProcessed { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }

        public List<string> FailedDocDetails { get; set; } = new List<string>();
    }
}
