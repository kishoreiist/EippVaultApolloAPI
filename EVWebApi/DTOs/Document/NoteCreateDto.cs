namespace EVWebApi.DTOs.Document
{
    public class NoteCreateDto
    {
        public string NoteText { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public int DocumentId { get; set; }
    }
}
