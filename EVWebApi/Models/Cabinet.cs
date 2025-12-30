using EVWebApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Cabinet {

    [Column("cabinet_id")]
    public int CabinetId { get; set; }
    [Column("cabinet_name")]
    public string CabinetName { get; set; }
    [Column("description")]
    public string Description { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public ICollection<GroupCabinet> GroupCabinets { get; set; }
}
