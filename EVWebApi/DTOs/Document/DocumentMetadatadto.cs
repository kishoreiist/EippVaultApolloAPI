namespace EVWebApi.DTOs.Document
{
    public class DocumentMetadatadto
    {
        public string FileName { get; set; }
        public string? InvoiceNumber { get; set; }


        public string? EmployeeId { get; set; }

        public string? ContactNumber { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? StatementDate { get; set; }
        public DateTime? DOJ { get; set; }
        public DateTime? DOB { get; set; }
        public decimal? Amount { get; set; }


        public decimal? PaidAmount { get; set; }
        public string? DocumentType { get; set; }
        public string? Region { get; set; }


        public int? ManufactureId { get; set; }//------------------now just id value, but need to keep fk relation
        public string? Name { get; set; }
        public string? Period { get; set; }
        public string? LoginId { get; set; }
        public string? LoginName { get; set; }
        public string? Remarks { get; set; }
    }
}