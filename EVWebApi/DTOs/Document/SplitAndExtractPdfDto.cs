namespace EVWebApi.DTOs.Document
{
    public class SplitAndExtractPdfDto
    {
        public int Id { get; set; }
        public DocumentSourceType Source { get; set; }
        public int CabinetId { get; set; }
        public required int FromPage { get; set; }
        public required int ToPage { get; set; }
        public required string DocumentType { get; set; }

    }
}
