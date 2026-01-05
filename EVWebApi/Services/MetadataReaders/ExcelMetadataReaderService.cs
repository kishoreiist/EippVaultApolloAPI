using ClosedXML.Excel;
using EVWebApi.DTOs.Document;
using EVWebApi.Interfaces.Services.MetaDataReaders;


namespace EVWebApi.Services.MetadataReaders
{
    public class ExcelMetadataReaderService : IMetadataReaderService
    {
        public bool CanRead(string fileExtension)
            => fileExtension is ".xlsx" or ".xls";

        public async Task<MetadataReadResultDTO<DocumentMetadatadto>> ReadAsync(IFormFile file)
        {
            var result = new MetadataReadResultDTO<DocumentMetadatadto>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var headerRow = sheet.Row(1);
            var headers = headerRow.Cells().Select(c => c.GetString().Trim().ToLower()).ToList();
            int row = 2; // header in row 1


            while (!sheet.Row(row).IsEmpty())
            {
                result.TotalRecords++;

                try
                {
                    var record = new DocumentMetadatadto();
                    bool hasRowError = false;

                    for (int col = 0; col < headers.Count; col++)
                    {
                        var header = headers[col];

                        var prop = typeof(DocumentMetadatadto)
                            .GetProperties()
                            .FirstOrDefault(p =>
                                p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

                        if (prop == null) continue;

                        var cell = sheet.Row(row).Cell(col + 1);
                        var cellValue = cell.GetValue<string>();

                        if (string.IsNullOrWhiteSpace(cellValue)) continue;

                        try
                        {
                            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            var convertedValue = Convert.ChangeType(cellValue, targetType);
                            prop.SetValue(record, convertedValue);
                        }
                        catch
                        {
                            hasRowError = true;
                            result.Errors.Add(
                                $"Row {row-1}: Invalid value '{cellValue}' for field '{prop.Name}'"
                            );
                        }
                    }

                    //result.Records.Add(record);
                    if (!hasRowError)
                    {
                        result.Records.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }

                row++;
            }

            return result;
        }
    }
}

