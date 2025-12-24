using System.ComponentModel.DataAnnotations;

namespace EVWebApi.DTOs.Document
{
    public class BatchUploadDTO
    {
        [Required]
        public int CabinetId { get; set; }

        [Required]
        public IFormFile MetadataFile { get; set; }

        [Required]
        public List<IFormFile> Files { get; set; }
    }

}
