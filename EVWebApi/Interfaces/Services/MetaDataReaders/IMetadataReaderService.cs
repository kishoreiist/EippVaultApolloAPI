using EVWebApi.DTOs.Document;

namespace EVWebApi.Interfaces.Services.MetaDataReaders
{
    public interface IMetadataReaderService
    {
        bool CanRead(string fileExtension);
        Task<MetadataReadResultDTO<DocumentMetadatadto>> ReadAsync(IFormFile file);
    }
}
