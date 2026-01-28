namespace EVWebApi.DTOs.Document
{
    public class DocumentStreamResultDTO
    {
        public Stream Stream { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public string FileName => Path.GetFileName(FilePath);
    }
}
