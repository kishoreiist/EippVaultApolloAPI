using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class Candidate
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("adhaar")]
        public string? Adhaar { get; set; }
        [Column("pan")]
        public string? PAN { get; set; }
        [Column("name")]
        public string? Name { get; set; }
        [Column("phone")]
        public string? Phone { get; set; }
        [Column("is_hired")]
        public bool IsHired { get; set; } = false;
        [Column("dob")]
        public DateTime? DateOfBirth { get; set; }

        [Column("is_laptop_requested")]
        public bool IsLaptopRequestSent { get; set; } = false;
        [Column("region")]
        public string? Region { get; set; }
    }
}
