using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{

    public class Metadata
    {
        [Column("metadata_id")]
        public int MetadataId { get; set; }
        [Column("document_id")]
        public int DocumentId { get; set; }
        [Column("meta_key")]
        public string MetaKey { get; set; }
        [Column("meta_value")]
        public string MetaValue { get; set; }
        public Document Document { get; set; }
    }
}