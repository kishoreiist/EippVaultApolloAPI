using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models.HR
{
    public class ConfigRequestRecipient
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("config_id")]
        public int RequestId { get; set; }
        public ConfigRequest Request { get; set; }
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
        [Column("emp_id")]
        public string? EmployeeId { get; set; }
        [Column("dob")]
        public DateTime? DateOfBirth { get; set; }
        [Column("token")]
        public string Token { get; set; }
        [Column("status")]
        public string Status { get; set; } = "Pending"; // Pending / InProgress / Completed / Expired
        [Column("accessed_at")]
        public DateTime? AccessedAt { get; set; }
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public ICollection<OnboardingDocument> UploadedDocuments { get; set; } = new List<OnboardingDocument>();
    }
}
