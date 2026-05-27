using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class EmailGroup
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("group_name")]
        public string GroupName { get; set; }
        [Column("is_external")]
        public bool IsExternal { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
