using EVWebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.HR
{
    public class CreateCollectionDto
    {
        public required string Name { get; set; }
        public string Designation { get; set; }
        public required string Region { get; set; }
        public string? Status { get; set; } = "active";
        public bool IsExternal { get; set; } = true;
        public required List<string> DocumentTypes { get; set; }
    }

    public class DocTypesDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CollectionResponseDto
    {
        public int Id { get; set; }
        public  string Name { get; set; }
        public bool IsExternal { get; set; } = true;
        public  string Region { get; set; }
        public string Designation { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
        public required List<string> DocumentTypes { get; set; }
    }

    public class CollectionQueryDto
    {

        [FromQuery(Name = "page_number")]
        public int PageNumber { get; set; } = 1;
        [FromQuery(Name = "page_size")]
        public int PageSize { get; set; } = 10;
        [FromQuery(Name = "name")]
        public string? Name { get; set; }
        [FromQuery(Name = "doc_type")]
        public string? DocType { get; set; }

        [FromQuery(Name = "is_external")]
        public bool? IsExternal { get; set; }
        [FromQuery(Name = "status")]
        public string? Status { get; set; }
    }

    public class CollectionListResponseDto:CollectionResponseDto
    {
        public int DocTypeCount { get; set; }
    }

    public class CollectionDropDownQueryDto
    {
        [FromQuery(Name = "is_external")]
        public bool? IsExternal { get; set; }
    }
}
