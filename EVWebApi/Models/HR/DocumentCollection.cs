using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class DocumentCollection
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("is_external")]
        public bool IsExternal { get; set; } = true;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("created_by")]
        public int CreatedBy { get; set; }
        [Column("designation")]
        public string Designation { get; set; }
        [Column("region")]
        public string Region { get; set; }
        [Column("status")]
        public string Status { get; set; }
        [Column("type")]
        public string Type { get; set; }

        // Navigation
        public ICollection<CollectionDocumentType> CollectionDocumentTypes { get; set; }
    }
}
