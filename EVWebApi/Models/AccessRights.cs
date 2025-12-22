using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class AccessRights
    {
        [Column("sno")]
        public int Id { get; set; }
        [Column("name")]
        public string AccessName { get; set; }
        [Column("status")]
        public Boolean Status { get; set; }
    }
}

