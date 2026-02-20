namespace EVWebApi.DTOs.Document
{
    public class SplitAndExtractPdfDto
    {
        public required int DocumentId { get; set; }
        public int CabinetId { get; set; }
        public required int FromPage { get; set; }
        public required int ToPage { get; set; }
        public required string DocumentType { get; set; }

    }
}
