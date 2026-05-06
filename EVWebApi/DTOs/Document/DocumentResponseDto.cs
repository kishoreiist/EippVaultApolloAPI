namespace EVWebApi.DTOs.Document
{
    public class DocumentResponseDto
    {
        public int DocumentId { get; set; }
        public int CabinetId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int? Version { get; set; }
        public string Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? DocumentType { get; set; }
        public List<MetadataDTO>? Metadata { get; set; }

        public string? InvoiceNumber { get; set; }

        public string? EmployeeId { get; set; }

        public string? ContactNumber { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Region { get; set; }

        public DateTime? InvoiceDate { get; set; }
        public DateTime? StatementDate { get; set; }
        public DateTime? DOJ { get; set; }
        public DateTime? DOB { get; set; }

        public decimal? Amount { get; set; }
       
   
        public decimal? PaidAmount { get; set; }
        public int NotesCount { get; set; }

        public int? ManufactureId { get; set; }//------------------now just id value, but need to keep fk relation
        public string? Name { get; set; }
        public string? Period { get; set; }
        public string? LoginId { get; set; }
        public string? LoginName { get; set; }
        public string? Remarks { get; set; }

        //public List<NotesDto>?Notes { get; set; } 
        public string[] Actions { get; internal set; }



    }

    public class ManfactureDto
    {
        public int Id { get; set; }
        public int ManfactureId { get; set; }
        public string ManfactureName { get; set; }
    }

}
