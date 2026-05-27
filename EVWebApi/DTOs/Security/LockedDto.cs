using EVWebApi.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Security
{
    public class LockedDto
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
        public string LockType { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public string UnlockedBy { get; set; }

    }
    public class LockedUserQueryParameters:QueryParameters
    {
        [FromQuery(Name ="name")]
        public string? Name { get; set; }
        [FromQuery(Name ="lock_type")]
        public string? LockType { get; set; }
        [FromQuery(Name ="reason")]
        public string? Reason { get; set; }
        [FromQuery(Name ="locked_from")]
        public DateTime? LockedFrom { get; set; }
        [FromQuery(Name = "locked_to")]
        public DateTime? LockedTo { get; set; }
        [FromQuery(Name ="status")]
        public string? Status { get; set; } // active | unlocked | all



    }
}
