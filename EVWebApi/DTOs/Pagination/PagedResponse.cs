namespace EVWebApi.DTOs.Pagination
{
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        //public int Offset { get; set; }
        //public int Limit { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
    public class GroupedPaginationResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalRows { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

    }
}
