namespace EVWebApi.DTOs.Pagination
{
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
    }

}
