using ClosedXML.Excel;
using EVWebApi.DTOs.Document;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using System.Globalization;


namespace EVWebApi.Services.MetadataReaders
{
    public class ExcelMetadataReaderService : IMetadataReaderService
    {
        public bool CanRead(string fileExtension)
            => fileExtension is ".xlsx" or ".xls";

        public async Task<MetadataReadResultDTO<T>> ReadAsync<T>(IFormFile file)
        {
            var result = new MetadataReadResultDTO<T>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var headerRow = sheet.Row(1);
            var headers = headerRow.Cells().Select(c => c.GetString().Trim().ToLower()).ToList();
            result.Headers = headers;
            //int row = 2; // header in row 1


            //while (!sheet.Row(row).IsEmpty())
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            for (int row = 2; row <= lastRow; row++)
            {
                if (sheet.Row(row).IsEmpty())
                    continue;
                result.TotalRecords++;

                try
                {
                    var record = Activator.CreateInstance<T>();
                    bool hasRowError = false;

                    for (int col = 0; col < headers.Count; col++)
                    {
                        var header = headers[col];

                        var prop = typeof(T)
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

                            object? convertedValue;

                            if (targetType == typeof(DateTime))
                            {
                                convertedValue = cell.GetDateTime();
                            }
                            else
                            {
                                //convertedValue =
                                //    Convert.ChangeType(cellValue, targetType);
                                if (targetType == typeof(int))
                                {
                                    convertedValue = int.Parse(cellValue);
                                }
                                else if (targetType == typeof(decimal))
                                {
                                    convertedValue = decimal.Parse(
                                        cellValue,
                                        CultureInfo.InvariantCulture);
                                }
                                else if (targetType == typeof(DateTime))
                                {
                                    convertedValue = DateTime.Parse(
                                        cellValue,
                                        CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    convertedValue = cellValue;
                                }
                            }


                            //var convertedValue = Convert.ChangeType(cellValue, targetType);
                            prop.SetValue(record, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            hasRowError = true;
                            result.Errors.Add(
                                $"Row {row-1}: Invalid value '{cellValue}' for field '{prop.Name}'.Error:{ex.Message}"
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

               
            }

            return result;
        }
    }
}

