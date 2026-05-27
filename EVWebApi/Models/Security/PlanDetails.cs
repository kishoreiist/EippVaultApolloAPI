using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.Security
{
    public class PlanDetails
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("storage_root")]
        public required string StorageRoot { get; set; }

        [Column("client_name")]
        public required string ClientName { get; set; }

        [Column("plan_size_bytes")]
        public long PlanSizeBytes { get; set; }
        [Column("used_size_bytes")]
        public long UsedSizeBytes { get; set; }

        [Column("first_alert_sent")]
        public bool FirstAlertSent { get; set; }
        [Column("final_alert_sent")]
        public bool FinalAlertSent { get; set; }
        [Column("last_alert_sent_at")]
        public DateTime? LastAlertSentAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

    }
}

