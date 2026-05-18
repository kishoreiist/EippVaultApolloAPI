using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class OnboardingDocument
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("request_id")]
        public int? RecipientId { get; set; }
        public ConfigRequestRecipient Recipient { get; set; }
        [Column("candidate_id")]
        public int CandidateId { get; set; }
        public Candidate Candidate { get; set; }
        [Column("document_type_id")]
        public int DocumentTypeId { get; set; }
        public DocumentTypes DocumentType { get; set; }
        
        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        [Column("status")]
        public string Status { get; set; }
        [Column("source")]
        public string Source { get; set; }


    }
}
