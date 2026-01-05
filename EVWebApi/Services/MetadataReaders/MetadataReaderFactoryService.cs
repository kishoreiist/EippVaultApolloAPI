using EVWebApi.Interfaces.Services.MetaDataReaders;
using System.Reflection.Metadata;

namespace EVWebApi.Services.MetadataReaders
{
    public class MetadataReaderFactoryService: IMetadataReaderFactoryService
    {
        private readonly IEnumerable<IMetadataReaderService> _readers;

        public MetadataReaderFactoryService(IEnumerable<IMetadataReaderService> readers)
        {
            _readers = readers;
        }

        public IMetadataReaderService GetReader(string fileExtension)
        {
            var reader = _readers.FirstOrDefault(r => r.CanRead(fileExtension));

            if (reader == null)
                throw new NotSupportedException($"Metadata format '{fileExtension}' is not supported.");

            return reader;
        }
    }
}
