using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class CollectionDocumentType
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("collection_id")]
        public int CollectionId { get; set; }
        public DocumentCollection Collection { get; set; }
        [Column("doc_type_id")]
        public int DocumentTypeId { get; set; }
        public DocumentTypes DocumentType { get; set; }

    }
}
