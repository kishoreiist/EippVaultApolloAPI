using CsvHelper;
using CsvHelper.Configuration;
using EVWebApi.DTOs.Document;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using System.Globalization;

namespace EVWebApi.Services.MetadataReaders
{
    public class CsvMetadataReaderService : IMetadataReaderService
    {
       public bool CanRead(string fileExtension)
            => fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
        public Task<MetadataReadResultDTO<DocumentMetadatadto>> ReadAsync(IFormFile file)
        => DelimitedFileReadHelper.ReadAsync(file, ',');

    }
}