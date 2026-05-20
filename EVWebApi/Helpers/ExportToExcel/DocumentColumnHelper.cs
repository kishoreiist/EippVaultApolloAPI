using EVWebApi.DTOs.Document;
using EVWebApi.Models;

namespace EVWebApi.Helpers.ExportToExcel
{
    public static class DocumentColumnHelper
    {
        public static readonly Dictionary<string, Func<Document, object?>> ColumnMap = new()
        {
            ["EmployeeId"] = d => d.EmployeeId,
            
            ["ContactNumber"] = d => d.ContactNumber,
            ["Designation"] = d => d.Designation,
            ["DOB"] = d => d.DOB,
            ["DOJ"] = d => d.DOJ,
            ["DocType"] = d => d.DocType,


            ["ManufactureId"] = d => d.ManufactureId,
            ["Manufacture Name"] = d => d.Name,
            ["LoginId"] = d => d.LoginId,
            ["InvoiceDate"] = d => d.InvoiceDate,
            ["Amount"] = d => d.Amount,
            ["InvoiceNumber"] = d => d.InvoiceNumber,
            ["LoginName"] =d=>d.LoginName,
            ["Remarks"] =d=>d.Remarks,
            ["Period"] =d=>d.Period

        };

        public static object? GetColumnValue(Document document, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return null;

            // convert snake_case to PascalCase
            var pascalCase = string.Concat(
                columnName.Split('_')
                          .Select(s => char.ToUpper(s[0]) + s.Substring(1))
            );
            if (ColumnMap.TryGetValue(pascalCase, out var selector))
            {
                return selector(document);
            }
            return null;
        }
    }
}
