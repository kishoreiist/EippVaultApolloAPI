using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class DocDownloadLink
    {
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        [Required]

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [MaxLength(200)]
        public string? PasswordHash { get; set; }

        [Required]
        public int MaxDownloads { get; set; } = 2;

        [Required]
        public int CurrentDownloads { get; set; } = 0;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
