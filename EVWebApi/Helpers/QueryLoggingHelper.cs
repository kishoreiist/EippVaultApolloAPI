using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EVWebApi.Helpers
{

    public static class QueryLoggingHelper
    {
        public static string ToFilterLog(this object query)
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
            "Files",
            "CabinetId",
            "PageNumber",
            "PageSize"

        };

            //// Build filters
            //var props = query.GetType().GetProperties()
            //    .Where(p => !ignoreFields.Contains(p.Name) &&
            //                p.GetValue(query) != null)
            //    .Select(p => $"{p.Name}='{p.GetValue(query)}'");
            var props = query.GetType().GetProperties()
                .Where(p => !ignoreFields.Contains(p.Name) &&
                    p.GetValue(query) != null)
                 .Select(p =>
                 {
                     var value = p.GetValue(query);

                     if (value is IEnumerable<string> list)
                         return $"{p.Name} : '{string.Join(", ", list)}'";

                     return $"{p.Name} : '{value}'";
                 });


            var result = string.Join(", ", props);

            return string.IsNullOrEmpty(result)
                ? "None"
                : $"{result}";
        }
    }
}

