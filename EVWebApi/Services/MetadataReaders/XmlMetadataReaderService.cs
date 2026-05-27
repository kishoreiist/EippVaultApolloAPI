using EVWebApi.DTOs.Document;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using System.Xml;
using System.Xml.Serialization;

namespace EVWebApi.Services.MetadataReaders
{
    public class XmlMetadataReaderService: IMetadataReaderService
    {
        public bool CanRead(string fileExtension)
            => fileExtension.Equals(".xml", StringComparison.OrdinalIgnoreCase);

        public async Task<MetadataReadResultDTO<T>> ReadAsync<T>(IFormFile file)
        {
            var result = new MetadataReadResultDTO<T>();

            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                Async = true
            };

            using var stream = file.OpenReadStream();
            using var reader = XmlReader.Create(stream, settings);
            int rowNumber = 1;

            var serializer = new XmlSerializer(
                    typeof(T),
                    new XmlRootAttribute("Document")
                    {
                        Namespace = ""
                    }
             );


            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.Name.Equals("Document", StringComparison.OrdinalIgnoreCase))
                {
                    result.TotalRecords++;
                    

                    try
                    {
                        using var subReader = reader.ReadSubtree();
                        var record = (T)serializer.Deserialize(subReader)!;
                        result.Records.Add(record);

                    }
                    catch (Exception ex)
                    {
                        var message = ex.InnerException?.Message ?? ex.Message;
                        result.Errors.Add(
                            $"Record {rowNumber}: {message}");

                    }

                    rowNumber++;
                }
            }

            return result;
        }
    }
}


