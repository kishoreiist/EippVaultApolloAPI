namespace EVWebApi.DTOs.Group
{
    public class CreateGroupDto
    {
        public string GroupName { get; set; } = null!;
        public GroupDescriptionDTO Description { get; set; } = new GroupDescriptionDTO();
    }

}
