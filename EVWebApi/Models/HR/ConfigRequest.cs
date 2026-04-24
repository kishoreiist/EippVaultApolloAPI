using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class ConfigRequest
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("collection_id")]
        public int CollectionId { get; set; }
        public DocumentCollection Collection { get; set; }
        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        [Column("created_by")]
        public int CreatedBy { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ConfigRequestRecipient> Recipients { get; set; } = new List<ConfigRequestRecipient>();
    }

}
