using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class GroupCabinet
    {
        [Column("group_id")]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        [Column("cabinet_id")]
        public int CabinetId { get; set; }
        public Cabinet Cabinet { get; set; }
    }
}
