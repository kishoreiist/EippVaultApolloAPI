using Syncfusion.XlsIO;

namespace EVWebApi.Helpers.ExportToExcel
{
    public class ExportExcelBuildHelper
    {
        public static byte[] BuildExcel<T>(
        List<T> data,
        List<string> columns,
        Func<T, string, object?> valueSelector)
        {
            using var excelEngine = new ExcelEngine();
            var app = excelEngine.Excel;
            app.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = app.Workbooks.Create(1);
            var sheet = workbook.Worksheets[0];

            // Header
            for (int col = 0; col < columns.Count; col++)
            {
                sheet.Range[1, col + 1].Text = columns[col];
            }

            sheet.Range[1, 1, 1, columns.Count].CellStyle.Font.Bold = true;

            int row = 2;
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            foreach (var item in data)
            {
                for (int col = 0; col < columns.Count; col++)
                {
                    var value = valueSelector(item, columns[col]);


                    if (value is DateTime dt)
                    {
                        var utc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        var istTime = TimeZoneInfo.ConvertTimeFromUtc(utc, istZone);

                        sheet.Range[row, col + 1].Text =
                            istTime.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    else
                    {
                        sheet.Range[row, col + 1].Text = value?.ToString();
                    }
                }

                row++;
            }

            sheet.UsedRange.AutofitColumns();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}
