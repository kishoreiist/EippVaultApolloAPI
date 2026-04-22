using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Pagination
{
    public class QueryParameters
    {
        [FromQuery(Name ="page_number")]
        public int PageNumber { get; set; } = 1;
        [FromQuery(Name = "page_size")]
        public int PageSize { get; set; } = 10;

        public string? search { get; set; }
        [FromQuery(Name = "from_date")]
        public DateTime? FromDate { get; set; }
        [FromQuery(Name = "to_date")]
        public DateTime? ToDate { get; set; }
    }

}
