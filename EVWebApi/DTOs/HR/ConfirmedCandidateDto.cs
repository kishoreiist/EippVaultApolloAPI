using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.HR
{
    public class ConfirmedCandidateDto
    {
        public int BatchId { get; set; }
        public int Inserted { get; set; }
        public int Skipped { get; set; }
        public string Message { get; set; }
    }
    public class  ConfirmIndivitualCandidateDto
    {
        public required int CandidateId { get; set; }
        public required string EmployeeId { get; set; }
        public required string Designation { get; set; }
        public required string Department { get; set; }
        public required DateTime DOJ { get; set; }
    }
    public class ExportOnboardingReportQuery
    {
        [FromQuery(Name = "report_type")]
        public string? ReportType { get; set; }

        [FromQuery(Name = "region")]
        public string? Region { get; set; }
        [FromQuery(Name = "status")]
        public string? Status { get; set; }
        [FromQuery(Name = "onboarding_type")]
        public string? OnboardingType { get; set; }

        [FromQuery(Name = "config_id")]
        public long? ConfigId { get; set; }
        [FromQuery(Name = "from_date")]
        public DateTime? FromDate { get; set; }

        [FromQuery(Name = "to_date")]
        public DateTime? ToDate { get; set; }
    }
    public class OnboardingReportRowDto
    {
        public long RequestId { get; set; }

        public string? CandidateName { get; set; }
        public string? OnboardingType { get; set; }

        public string? Email { get; set; }

        public string? Region { get; set; }

        public string? OverAllStatus { get; set; }
        public bool IsHired { get; set; }

        public decimal CompletionPercent { get; set; }

        public string? DocumentName { get; set; }

        public string? DocumentStatus { get; set; }
    }
    public class OnboardingReportExportDto
    {
        public string? CandidateName { get; set; }

        public string? Email { get; set; }

        public string? Region { get; set; }

        public string? OverAllStatus { get; set; }
        public string? OnboardingType { get; set; }
        public bool IsHired{ get; set; }

        public decimal CompletionPercent { get; set; }

        public Dictionary<string, string> Documents { get; set; } = new();
    }
}
