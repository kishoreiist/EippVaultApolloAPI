using EVWebApi.DTOs.Group;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace EVWebApi.Models
{
    public class Group
    {
        [Column("group_id")]
        public int GroupId { get; set; }
        [Column("group_name")]
        public required string GroupName { get; set; }

        [Column("user_type")]
        public string UserType { get; set; }

        [Column("region")]
        public string? Region { get; set; }
        public DateTime CreatedAt { get; set; }
        public  ICollection<UserGroup>? UserGroups { get; set; } = new List<UserGroup>();
        public ICollection<GroupAccessRight> GroupAccessRights { get; set; }
        public ICollection<GroupCabinet> GroupCabinets { get; set; }
    }
}
