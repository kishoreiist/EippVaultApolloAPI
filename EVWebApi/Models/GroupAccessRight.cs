using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class GroupAccessRight
    {
        [Column("group_id")]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        [Column("access_id")]
        public int AccessId { get; set; }
        public AccessRights AccessRight { get; set; }
    }
}
