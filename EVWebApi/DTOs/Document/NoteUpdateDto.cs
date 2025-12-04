namespace EVWebApi.DTOs.Document
{
    public class NoteUpdateDto
    {
        public string NoteText { get; set; } 
        public required int NoteId { get; set; }
    }
}
