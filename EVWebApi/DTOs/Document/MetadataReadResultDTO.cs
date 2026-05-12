namespace EVWebApi.DTOs.Document
{
    public class MetadataReadResultDTO<T>
    {
        public List<string> Headers { get; set; } = new();
        public List<T> Records { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int TotalRecords { get; set; }



    }
}
