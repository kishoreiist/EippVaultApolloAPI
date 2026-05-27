using EVWebApi.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Group
{
    public class GroupQueryParameters: QueryParameters
    {
        [FromQuery(Name = "group_name")]
        public string? Groupname { get; set; }
        [FromQuery(Name = "region")]
        public string? Region { get; set; }
    }
}
