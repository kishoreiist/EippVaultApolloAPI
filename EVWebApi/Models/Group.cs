namespace EVWebApi.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        public required string GroupName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public required ICollection<UserGroup> UserGroups { get; set; }
    }
}
