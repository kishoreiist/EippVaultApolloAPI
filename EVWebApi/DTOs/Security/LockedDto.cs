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
        public DateTimeOffset LockedAt { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
        public DateTimeOffset? UnlockAt { get; set; }
        public int UnlockBy { get; set; }
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
        public DateTimeOffset? LockedFrom { get; set; }
        [FromQuery(Name = "locked_to")]
        public DateTimeOffset? LockedTo { get; set; }



    }
}
