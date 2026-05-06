using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public class ManufactureDetails
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("manufacture_id")]
        public int ManufactureId { get; set; }
        [Column("manufacture_name")]
        public string ManufactureName { get; set; } = string.Empty;
    }
}
