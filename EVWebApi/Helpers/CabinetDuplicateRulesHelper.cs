using EVWebApi.DTOs.Document;

namespace EVWebApi.Helpers
{
    public static class CabinetDuplicateRulesHelper
    {
        private static readonly Dictionary<int, string[]> _rules =
        new()
        {
            // CabinetId → Fields that define duplication
            { 1, new[] { "InvoiceNumber", "Amount", "InvoiceDate" } },
            { 2, new[] { "EmployeeId" ,  "ContactNumber" } },
            //{ 3, new[] { "Name", "Amount", "StatementDate" } },
            //{ 4, new[] { "VendorNumber", "Name",  "PoNumber",  "Amount", "InvoiceDate" } },
            { 5, new[] { "Name", "ManufactureId", "LoginId", "LoginName", "Period" } }
           // { 5, new[] { "Name", "ManufactureId", "Period" } }
        };

        public static bool TryGetRules(int cabinetId, out string[] fields)
        {
            return _rules.TryGetValue(cabinetId, out fields!);
        }


        public static List<string> ValidateMandatoryFields(int cabinetId, DocumentUploadDto dto)
        {
            var missingFields = new List<string>();

            if (!TryGetRules(cabinetId, out var fields))
                return missingFields;

            foreach (var field in fields)
            {
                var hasValue = field switch
                {
                    "InvoiceNumber" => !string.IsNullOrWhiteSpace(dto.InvoiceNumber),
                    "Amount" => dto.Amount != null,
                    "InvoiceDate" => dto.InvoiceDate != null,
                    "EmployeeId" => !string.IsNullOrWhiteSpace(dto.EmployeeId),
                    "ContactNumber" => !string.IsNullOrWhiteSpace(dto.ContactNumber),
                    "Name" => !string.IsNullOrWhiteSpace(dto.Name),
                    "StatementDate" => dto.StatementDate != null,
                    "ManufactureId" => dto.ManufactureId!=null,
                    //"LoginId" => !string.IsNullOrWhiteSpace(dto.LoginId),
                    //"LoginName" => !string.IsNullOrWhiteSpace(dto.LoginName),
                    "Period" => dto.Period != null,
                    _ => true // ignore unknown fields
                };

                if (!hasValue)
                    missingFields.Add(field);
            }

            return missingFields;
        }
    }
}


