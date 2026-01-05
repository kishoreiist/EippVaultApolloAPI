using EVWebApi.DTOs.Document;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using EVWebApi.Services.MetadataReaders;

namespace EVWebApi.Services.MetadataReaders
{
    public class TxtMetadataReaderService : IMetadataReaderService
    {
        //private readonly CsvMetadataReaderService _csvReader;

        //public TxtMetadataReaderService(CsvMetadataReaderService csvReader)
        //{
        //    _csvReader = csvReader;
        //}

        public bool CanRead(string fileExtension)
            => fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase);

        public async Task<MetadataReadResultDTO<DocumentMetadatadto>> ReadAsync(IFormFile file)
        {
            char? delimiter = await DetectDelimiterAsync(file);

            if (delimiter == null)
            {
                return new MetadataReadResultDTO<DocumentMetadatadto>
                {
                    Errors =
                {
                    "Unable to detect TXT delimiter. Supported delimiters: , | ; tab."
                }
                };
            }

            return await DelimitedFileReadHelper.ReadAsync(file, delimiter.Value);
        }

        private static async Task<char?> DetectDelimiterAsync(IFormFile file)
        {
            var supported = new[] { ',', '|', ';', '\t' };
            var lines = new List<string>();

            using var reader = new StreamReader(file.OpenReadStream());
            while (!reader.EndOfStream && lines.Count < 5)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            var candidates = supported
                .Select(d => new
                {
                    Delimiter = d,
                    Counts = lines.Select(l => l.Split(d).Length).Distinct().ToList()
                })
                .Where(x => x.Counts.Count == 1 && x.Counts[0] > 1)
                .ToList();

            return candidates.Count == 1 ? candidates[0].Delimiter : null;
        }
    }
}

