using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.DTOs.Document
{
    public class UpdateDocumentDto
    {
        public int CabinetId { get; set; }    
        public string? FileName { get; set; }
        public string? FilePath { get; set; }

        public string? MetadataJson { get; set; }

        //[NotMapped]
        //public List<MetadataDTO>? Metadata { get; set; }


        public string? InvoiceNumber { get; set; }
         public string? PoNumber { get; set; }
         public string? VendorNumber { get; set; }
         public string? EmployeeId { get; set; }
         public string? Name { get; set; }
         public string? ContactNumber { get; set; }
         public string? Designation { get; set; }
         public string? Department { get; set; }
         public string? Region { get; set; }

         public DateTime? InvoiceDate { get; set; }
         public DateTime? StatementDate { get; set; }
         public DateTime? DOJ { get; set; }
         public DateTime? DOB { get; set; }

         public decimal? Amount { get; set; }
         public decimal? GST { get; set; }
         public string? CheckNumber { get; set; }
         public decimal? PaidAmount { get; set; }
        
    }
}
