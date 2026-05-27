using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.Document
{
    public class AutoSuggestionRequestDto
    {
        [FromQuery(Name = "field_name")]
        public string FieldName { get; set; } = string.Empty;
        [FromQuery(Name = "search_text")]
        public string SearchText { get; set; } = string.Empty;
        [FromQuery(Name = "limit")]
        public int Limit { get; set; } = 10;
    }
}
