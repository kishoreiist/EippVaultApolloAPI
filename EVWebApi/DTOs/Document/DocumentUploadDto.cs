using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace EVWebApi.DTOs.Document
{ 
public class DocumentUploadDto
{
        public int CabinetId { get; set; }
        public IFormFile File { get; set; }

        //public List<MetadataDTO>? Metadata { get; set; }

        public string? MetadataJson { get; set; }
        [NotMapped]

        public List<MetadataDTO>? Metadata
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MetadataJson))
                {
                    return new List<MetadataDTO>();
                }

                try
                {
                    string trimmedJson = MetadataJson.Trim();

                    if (trimmedJson.StartsWith("["))
                    {
                        // Case 1: Client sent an array (Correct format received)
                        return JsonSerializer.Deserialize<List<MetadataDTO>>(trimmedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else if (trimmedJson.StartsWith("{"))
                    {
                        // Case 2: Client stripped the brackets (What you are seeing)
                        string arrayWrapperJson = $"[{trimmedJson}]";
                        return JsonSerializer.Deserialize<List<MetadataDTO>>(arrayWrapperJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else
                    {
                        return new List<MetadataDTO>();
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON Deserialization Error: {ex.Message}"); 
                    return new List<MetadataDTO>();
                }
            }
        }
    }

}
