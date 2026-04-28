using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.HR
{
    public class HrDashboardQueryParameters
    {

        [FromQuery(Name = "region")]
        public string? Region { get; set; }
    }

    public class PoDashboardQueryParameters
    {
        [FromQuery(Name = "page_number")]
        public int PageNumber { get; set; } = 1;
        [FromQuery(Name = "page_size")]
        public int PageSize { get; set; } = 10;
    }
}
