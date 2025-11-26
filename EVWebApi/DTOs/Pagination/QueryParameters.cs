namespace EVWebApi.DTOs.Pagination
{
    public class QueryParameters
    {
        public int Offset { get; set; } = 0;   // number of records to skip
        public int Limit { get; set; } = 100; // number of records to fetch
        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

}
