using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class DocAccessRequest
    {
        [Column("request_id")]
        public int RequestId { get; set; }

        [Required]
        [Column("doc_link_id")]
        public int DocLinkId { get; set; }

        [Required]
        [Column("document_id")]
        public int DocumentId { get; set; }

        [Required]
        [Column("requested_by")]
        public int RequestedBy { get; set; }

        [Required]
        [Column("requested_to")]
        public int RequestedTo { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }


        //  Navigation Properties

        [ForeignKey(nameof(DocLinkId))]
        public DocDownloadLink? DocDownloadLink { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        [ForeignKey(nameof(RequestedBy))]
        public User? RequestedByUser { get; set; }

        [ForeignKey(nameof(RequestedTo))]
        public User? RequestedToUser { get; set; }
    }
}
