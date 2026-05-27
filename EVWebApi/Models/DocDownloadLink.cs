using EVWebApi.Models.HR;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class DocDownloadLink
    {
        [Column("id")]
        public int Id { get; set; }

        //[Required]
        [Column("document_id")]
        public int? DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        [Column("onboarding_doc_id")]
        public int? OnboardingDocId { get; set; }

        [ForeignKey(nameof(OnboardingDocId))]
        public OnboardingDocument? OnboardingDocument { get; set; }

        [Required]
        [Column("assigned_to")]
        public int AssignedTo { get; set; }

        [ForeignKey(nameof(AssignedTo))]
        public User? AssignedUser { get; set; }

        [Required]
        [Column("created_by")]
        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedUser { get; set; }
        [MaxLength(200)]
        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Required]
        [Column("max_downloads")]
        public int MaxDownloads { get; set; } = 2;

        [Required]
        [Column("current_downloads")]
        public int CurrentDownloads { get; set; } = 0;

        [Required]
        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [Column("shared_batch_id")]
        public Guid SharedBatchId { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
