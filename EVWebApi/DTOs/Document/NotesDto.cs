using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.DTOs.Document
{
    public class NotesDto
    {

        public long NoteId { get; set; }

        public int DocumentId { get; set; }

        public string NoteText { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } 

    }
}
