namespace EVWebApi.DTOs.Cabinet
{
    public class CabinetDto
    {
        public int CabinetId { get; set; }
        public required string CabinetName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
