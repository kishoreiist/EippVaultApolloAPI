namespace EVWebApi.DTOs.Group
{
    public class CreateGroupDto
    {
        public string GroupName { get; set; } = null!;
        //public GroupDescriptionDTO Description { get; set; } = new GroupDescriptionDTO();
        public string UserType { get; set; }
        public List<int> AccessList { get; set; }
        public List<int> CabinetsList { get; set; }
    }

}
