namespace EVWebApi.Helpers
{
    public static class QueryLoggingHelper
    {
        public static string ToFilterLog(this object query)
        {
            if (query == null) return "No filters applied.";

            var props = query.GetType().GetProperties()
                .Where(p => p.GetValue(query) != null)
                .Select(p => $"{p.Name}='{p.GetValue(query)}'");

            var result = string.Join(", ", props);

            return string.IsNullOrEmpty(result)
                ? "No filters applied."
                : $"Search Filters applied: {result}";
        }
    }
}
