namespace EVWebApi.DTOs.HR
{
    public class HrParsedRowDto
    {
        public string? EmployeeId { get; set; }

        public string? Designation { get; set; }

        public DateTime? DOJ { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? PAN { get; set; }

        public string? Aadhaar { get; set; }
        public string? FileName { get; set; }

        public DateTime? DOB { get; set; }
    }

    public class OnboardingUploadDto
    {
        public required IFormFile File { get; set; }
    }

    public class HrUploadResponseDto
    {
        public int BatchId { get; set; }

        public int TotalRows { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<RowResponseDto> Records { get; set; }
            = new();
    }

    public class RowResponseDto
    {
        public int RowNumber { get; set; }

        public long? CandidateId { get; set; }

        public string? CandidateName { get; set; }

        public string? EmployeeId { get; set; }

        public string Status { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
