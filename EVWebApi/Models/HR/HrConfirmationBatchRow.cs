using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class HrConfirmationBatchRow
    {
        [Column("id")]
        public int RowId { get; set; }
        
        [Column("batch_id")]
        public int BatchId { get; set; }
        [ForeignKey("BatchId")]
        public HrConfirmationBatch Batch { get; set; }
        [Column("row_number")]
        public int RowNumber { get; set; }
        
        [Column("candidate_id")]
        public int? CandidateId { get; set; }
        [Column("employee_id")]
        public string? EmployeeId { get; set; }
        [Column("designation")]
        public string? Designation { get; set; }
        [Column("department")]
        public string? Department { get; set; }
        [Column("doj")]
        public DateTime? DOJ { get; set; }
        [Column("candidate_name")]
        public string? CandidateName { get; set; }
        
        [Column("email")]
        public string? Email { get; set; }
        [Column("phone")]
        public string? Phone { get; set; }
        [Column("pan")]
        public string? PAN { get; set; }
        [Column("aadhaar")]
        public string? Aadhaar { get; set; }
        [Column("dob")]
        public DateTime? DOB { get; set; }
        [Column("status")]
        public string Status { get; set; } = string.Empty;
        [Column("error_message")]
        public string? ErrorMessage { get; set; }
        [Column("is_confirmed")]
        public bool IsConfirmed { get; set; }

        [Column("confirmed_at")]
        public DateTime? ConfirmedAt { get; set; }

        // Optional navigation
        public Candidate? Candidate { get; set; }
    }
}
