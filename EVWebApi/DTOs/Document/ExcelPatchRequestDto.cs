namespace EVWebApi.DTOs.Document
{
    public class ExcelPatchRequestDto
    {
        public int DocumentId { get; set; }
        public DocumentSourceType Source { get; set; }
        public int CabinetId { get; set; }
        public List<ExcelCellPatchDto> Changes { get; set; } = new();
    }

    public class ExcelCellPatchDto
    {
        public string Address { get; set; } = string.Empty; 
        public object? Value { get; set; }
    }
}
