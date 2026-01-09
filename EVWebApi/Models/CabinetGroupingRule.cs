using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class CabinetGroupingRule
    {
        public int Id { get; set; }
  
        [Column("grouping_columns")]
        public string GroupingCol { get; set; }
        [Column("grouping_order")]
        public string GroupingOrder { get; set; }

        // Navigation Property
        [ForeignKey("cabinet_id")]
        public virtual Cabinet Cabinet { get; set; }
    }
}
