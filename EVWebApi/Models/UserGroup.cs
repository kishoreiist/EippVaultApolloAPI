using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class UserGroup
    {
        [Column("user_id")]
        public int UserId { get; set; }
        public  User? User { get; set; }

        [Column("group_id")]
        public int GroupId { get; set; }
        public Group? Group { get; set; }
    }
}
