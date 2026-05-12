using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class HrConfirmationBatch
    {
        [Column("id")]
        public int BatchId { get; set; }

        [Column("file_name")]
        public string FileName { get; set; } = string.Empty;
        [Column("uploaded_by")]
        public int UploadedBy { get; set; }
        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }
        [Column("status")]
        public string Status { get; set; } = "pending";
        
        [Column("total_rows")]
        public int TotalRows { get; set; }
        [Column("success_count")]   
        public int SuccessCount { get; set; }
        [Column("failure_count")]
        public int FailureCount { get; set; }
        [Column("remarks")]
        public string? Remarks { get; set; }

        public ICollection<HrConfirmationBatchRow> Rows { get; set; }
            = new List<HrConfirmationBatchRow>();
    }
}
