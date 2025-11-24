namespace EVWebApi.DTOs.Document
{
    public class DocumentDownloadDto
    {
        public Stream Stream { get; set; } = default!;
        public string FileName { get; set; } = string.Empty;
    }
}
