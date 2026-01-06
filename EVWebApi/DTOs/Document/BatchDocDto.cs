namespace EVWebApi.DTOs.Document
{
    public class BatchDocDto
    {
        public List<int> DocumentIds { get; set; } = new List<int>();
        public int CabinetId { get; set; }
    }
}
