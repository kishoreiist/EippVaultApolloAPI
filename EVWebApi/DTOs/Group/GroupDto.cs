namespace EVWebApi.DTOs.Group
{
    public class GroupDto
    {
        public int GroupId { get; set; }
        public required string GroupName { get; set; }
        public string? Region { get; set; }
        //public GroupDescriptionDTO? Description { get; set; }
        public string UserType { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ListDto> AccessList { get; set; }
        public List<ListDto> CabinetsList { get; set; }
    }

    public class EmailGroupDto
    {
        public int Id { get; set; }
        public required string GroupName { get; set; }       
        public DateTime? CreatedAt { get; set; }
        public bool IsExternal { get; set; }

    }
}
