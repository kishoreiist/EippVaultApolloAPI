namespace EVWebApi.DTOs.Pagination
{
    public class QueryParameters
    {
        //public int PageNumber { get; set; } = 1;
        //public int PageSize { get; set; } = 10;
        public int Offset { get; set; } = 0;   // number of records to skip
        public int Limit { get; set; } = 100; // number of records to fetch
        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

}
