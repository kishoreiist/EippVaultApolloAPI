using System.ComponentModel.DataAnnotations.Schema;
namespace EVWebApi.Models
{
    public class Document
    {

        [Column("document_id")]
        public int DocumentId { get; set; }
        [Column("cabinet_id")]
        public int CabinetId { get; set; }
        [Column("file_name")]
        public string FileName { get; set; }
        [Column("file_path")]
        public string FilePath { get; set; }
        [Column("version")]
        public int Version { get; set; }
        [Column("uploaded_by")]
        public int UploadedBy { get; set; }
        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }
        [Column("status")]
        public string Status { get; set; }
    }
}