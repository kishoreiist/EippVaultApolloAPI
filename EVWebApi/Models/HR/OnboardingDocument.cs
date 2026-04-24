using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class OnboardingDocument
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("recipient_id")]
        public int RecipientId { get; set; }
        public ConfigRequestRecipient Recipient { get; set; }
        [Column("document_type_id")]
        public int DocumentTypeId { get; set; }
        public DocumentTypes DocumentType { get; set; }
        
        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
