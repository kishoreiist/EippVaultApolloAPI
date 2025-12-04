using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Pagination
{
    public class QueryParameters
    {
        //public int Offset { get; set; } = 0;   // number of records to skip
        //public int Limit { get; set; } = 100; // number of records to fetch
        [FromQuery(Name ="page_number")]
        public int PageNumber { get; set; } = 1;
        [FromQuery(Name = "page_size")]
        public int PageSize { get; set; } = 10;

        public string? search { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

}
