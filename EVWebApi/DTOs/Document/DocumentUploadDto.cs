using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EVWebApi.DTOs.Document
{
    public class DocumentUploadDto
    {
        public int CabinetId { get; set; }
        public IFormFile File { get; set; }
      
        public string? InvoiceNumber { get; set; }
        public string? PoNumber { get; set; }
        public string? VendorNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? StatementDate { get; set; }
        public DateTime? DOJ { get; set; }
        public DateTime? DOB { get; set; }
        public decimal? Amount { get; set; }
        public decimal? GST { get; set; }
        public string? CheckNumber { get; set; }
        public decimal? PaidAmount { get; set; }

        public string? Region { get; set; }


    }

}
