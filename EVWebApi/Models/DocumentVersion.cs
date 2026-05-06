using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class DocumentVersion
    {
        [Column("document_id")]
        public int DocumentId { get; set; }

        [Column("version_id")]
        public int VersionId { get; set; }

        [Column("version_number")]
        public int VersionNo { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }

        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }

        [Column("file_path")]
        public string? FilePath { get; set; }

        [Column("invoice_number")]
        public string? InvoiceNumber { get; set; }




        [Column("employee_id")]
        public string? EmployeeId { get; set; }


        [Column("contact_number")]
        public string? ContactNumber { get; set; }

        [Column("designation")]
        public string? Designation { get; set; }

        [Column("department")]
        public string? Department { get; set; }

        [Column("region")]
        public string? Region { get; set; }

        [Column("invoice_date")]
        public DateTime? InvoiceDate { get; set; }

        [Column("statement_date")]
        public DateTime? StatementDate { get; set; }

        [Column("doj")]
        public DateTime? DOJ { get; set; }

        [Column("dob")]
        public DateTime? DOB { get; set; }

        [Column("amount")]
        public decimal? Amount { get; set; }


        [Column("paid_amount")]
        public decimal? PaidAmount { get; set; }

        [Column("action")]
        public string? Action { get; set; }
        [Column("status")]
        public string Status { get; set; }




        [Column("manufacture_id")]
        public int? ManufactureId { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("period")]
        public DateOnly? Period { get; set; }

        [Column("login_id")]
        public string? LoginId { get; set; }

        [Column("login_name")]
        public string? LoginName { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }


        [NotMapped]
        public Document DocVersion { get; set; }
    }
}
