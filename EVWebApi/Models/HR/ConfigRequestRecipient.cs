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

        [Column("candidate_id")]
        public int CandidateId { get; set; }
        public Candidate Candidate { get; set; }

        [Column("token")]
        public string Token { get; set; }
        
        [Column("accessed_at")]
        public DateTime? AccessedAt { get; set; }
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
        [Column("status")]
        public string Status { get; set; } = "pending"; // Pending / InProgress / Completed / Expired


        // Navigation
        public ICollection<OnboardingDocument> UploadedDocuments { get; set; } = new List<OnboardingDocument>();
    }
}
