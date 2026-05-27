using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.DTOs.Document
{
    public class BatchDocDto
    {
        public List<int> DocumentIds { get; set; } = new List<int>();
        public int CabinetId { get; set; }
    }

    public class ExportExcelDocDto
    {
        public List<int>? DocumentIds { get; set; } = new List<int>();
        public int CabinetId { get; set; }
    }


    public class DocumentExportDto
    {
        public int DocumentId { get; set; }
        public int? OnboardingDocId { get; set; }

        public int? CandidateId { get; set; }

        public string? EmployeeId { get; set; }
        public string? Name { get; set; }
        public string? Designation { get; set; }
        public string? ContactNumber { get; set; }
        public DateTime? DOJ { get; set; }
        public DateTime? DOB { get; set; }





        public int? ManufactureId { get; set; }
        public DateOnly? Period { get; set; }
        public string? LoginId { get; set; }
        public string? LoginName { get; set; }

        public string? Remarks { get; set; }
        public string? DocumentType { get; set; }

        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }
}
