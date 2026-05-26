
using EVWebApi.Models.HR;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class DocumentTypes
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("key")]
        public string Key { get; set; }
        [Column("active")]
        public Boolean Status { get; set; }
        [Column("label")]
        public string Label { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("category")]
        public string? Category { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();//one to many
        public ICollection<CollectionDocumentType> CollectionDocumentTypes { get; set; }
    }

}