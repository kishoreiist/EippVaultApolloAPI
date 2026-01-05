using CsvHelper;
using CsvHelper.Configuration;
using EVWebApi.DTOs.Document;
using System.Globalization;

namespace EVWebApi.Helpers
{
    public class DelimitedFileReadHelper
    {
        public static async Task<MetadataReadResultDTO<DocumentMetadatadto>> ReadAsync(
            IFormFile file,
            char delimiter)
        {
            var result = new MetadataReadResultDTO<DocumentMetadatadto>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(),
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                PrepareHeaderForMatch = args => args.Header.ToLower()
            };

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();

            int rowNumber = 2;

            while (await csv.ReadAsync())
            {
                result.TotalRecords++;

                try
                {
                    var record = csv.GetRecord<DocumentMetadatadto>();
                    result.Records.Add(record);
                }
                catch (CsvHelper.TypeConversion.TypeConverterException ex)
                {
                    var fieldName = ex.MemberMapData?.Member?.Name ?? "Unknown Field";
                    var badValue = ex.Text ?? "Empty";

                    result.Errors.Add(
                        $"Row {rowNumber-1}: Invalid value '{badValue}' for field '{fieldName}'.");
                }

                rowNumber++;
            }

            return result;
        }
    }
}
