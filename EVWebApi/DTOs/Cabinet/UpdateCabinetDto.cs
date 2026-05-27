namespace EVWebApi.DTOs.Cabinet
{
    public class UpdateCabinetDto
    {
        public required int CabinetId { get; set; }
        public string? CabinetName { get; set; }
        public string? Description { get; set; }
    }

}
