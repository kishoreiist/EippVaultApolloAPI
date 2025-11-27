using EVWebApi.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Cabinet
{
    public class CabinetQueryParameters : QueryParameters
    {
        [FromQuery(Name = "cabinet_name")]
        public string? CabinetName { get; set; }
    }
}
