using System.Reflection.Metadata;

namespace EVWebApi.Interfaces.Services.MetaDataReaders
{
    public interface IMetadataReaderFactoryService
    {
        IMetadataReaderService GetReader(string fileExtension);
    }
}
