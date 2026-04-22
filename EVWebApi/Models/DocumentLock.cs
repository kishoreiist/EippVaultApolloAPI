using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class DocumentLock
    {

        [Column("document_id")]
        public int DocumentId { get; set; }

        [Column("lock_id")]
        public int LockId { get; set; }

        [Column("locked_by")]
        public int LockedBy { get; set; }

        [Column("locked_at")]
        public DateTime LockedAt { get; set; }

        [Column("lock_expiry")]
        public DateTime LockExpiry { get; set; }
    }
}
