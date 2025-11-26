namespace EVWebApi.DTOs.Group
{
    public class GroupDescriptionDTO
    {
        public List<string> Cabinets { get; set; } = new List<string>();
        public string UserType { get; set; } = string.Empty;
        public List<string> AccessList { get; set; } = new List<string>();
    }
}
