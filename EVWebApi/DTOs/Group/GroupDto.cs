namespace EVWebApi.DTOs.Group
{
    public class GroupDto
    {
        public int GroupId { get; set; }
        public required string GroupName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        //public DateTime UpdatedAt { get; set; }
    }
}
