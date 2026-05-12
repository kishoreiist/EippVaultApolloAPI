namespace EVWebApi.DTOs.Document
{
    public class DocumentStreamResultDTO
    {
        public Stream Stream { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public string FileName { get; set; } = default!;
    }

    public class DocumentStreamInpDto
    {
        public int DocumentId { get; set; }
        public int? OnboardingDocId { get; set; }
    }

    public class DocumentRequestDto
    {
        public int Id { get; set; }
        public DocumentSourceType Source { get; set; }
    }

    public enum DocumentSourceType
    {
        Document,
        Onboarding
    }
    public class ExportDto
    {
        public int CabinetId { get; set; }
        public List<DocumentRequestDto> Documents { get; set; }
    }
}
