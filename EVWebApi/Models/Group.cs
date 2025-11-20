using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class Group
    {
        [Column("group_id")]
        public int GroupId { get; set; }
        [Column("group_name")]
        public required string GroupName { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        //public DateTime UpdatedAt { get; set; }
        public  ICollection<UserGroup>? UserGroups { get; set; }
    }
}
