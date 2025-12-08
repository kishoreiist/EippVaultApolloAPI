using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EVWebApi.Helpers
{

    public static class QueryLoggingHelper
    {
        public static string ToFilterLog(this object query, string prefix = "Search Filters applied - ")
        {
            if (query == null) return "No Filters Applied";

            // Fields to ignore
            var ignoreFields = new HashSet<string>
        {
            "Offset",
            "Limit",
            "SearchType",
            "AmountType",
            "DateType",
            "Body",          
            "AttachmentPaths",
            "Token",
            "Password",
            "TwoFactorCode",
            "TwoFactorRecoveryCode",
            "Code",
            "File",
            "CabinetId",
            "PageNumber",
            "PageSize",
            "ResetCode",
            "NewPassword"

        };

            var props = query.GetType().GetProperties()
                .Where(p => !ignoreFields.Contains(p.Name) &&
                    p.GetValue(query) != null)
                 .Select(p =>
                 {
                     var value = p.GetValue(query);
                     //to remove null values
                     if (value == null) return null;
                     if (value is string str && string.IsNullOrWhiteSpace(str)) return null;

                     if (value is IEnumerable<string> list && !(value is string))
                     {
                         var items = list.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                         if (items.Count == 0) return null;
                         return $"{p.Name} : '{string.Join(", ", list)}'";
                     }
                     return $"{p.Name} : '{value}'";
                 }).Where(x => x != null);


            var result = string.Join(", ", props);

            return string.IsNullOrEmpty(result)
                ? ""
                : $"{prefix}{result}";
        }
    }
}

