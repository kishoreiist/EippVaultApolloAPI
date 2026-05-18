using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.HR
{
    public class InternalUploaddto
    {

            public int? RecipientId { get; set; }
            public int CandidateId { get; set; }

            public int DocumentTypeId { get; set; }

            public string FileName { get; set; }

            public string TempFilePath { get; set; }

            public string Source { get; set; } = "hr_manual";
        
    }


    public class StatusCountResponseDto
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int  Completed{ get; set; }
        public int  InProgress { get; set; }
        public int  Expired { get; set; }
    }

    public class StatusCountQueryParamDto
    {
        [FromQuery(Name = "from_date")]
        public DateTime? FromDate { get; set; }

        [FromQuery(Name = "to_date")]
        public DateTime? ToDate { get; set; }

        [FromQuery(Name = "region")]
        public string? Region { get; set; }
        [FromQuery(Name = "onboarding_type")]
        public string? OnboardingType { get; set; }
    }
}
