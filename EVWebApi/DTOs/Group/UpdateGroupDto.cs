namespace EVWebApi.DTOs.Group
{
    public class UpdateGroupDto
    {
        public required int GroupId { get; set; }   
        public required string GroupName { get; set; }
        public required GroupDescriptionDTO Description { get; set; }
    }
}
