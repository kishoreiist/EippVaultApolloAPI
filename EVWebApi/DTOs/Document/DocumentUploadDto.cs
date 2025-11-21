using Swashbuckle.AspNetCore.SwaggerGen;

namespace EVWebApi.DTOs.Document
{ 
public class DocumentUploadDto
{
    public int CabinetId { get; set; }
    public IFormFile File { get; set; }
    public int UploadedBy { get; set; }
    public List<MetadataDTO>? Metadata { get; set; }
    }
}
