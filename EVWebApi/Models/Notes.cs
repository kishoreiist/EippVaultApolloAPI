using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class Notes
    {
        [Column("note_id")]
        public long NoteId { get; set; }
        [Column("document_id")]
        public int DocumentId { get; set; }
        [Column("note_text")]
        public string NoteText { get; set; }
        [Column("created_by")]
        public string CreatedBy { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public  Document Document { get; set; }
    }
}
