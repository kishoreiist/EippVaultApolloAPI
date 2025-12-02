namespace EVWebApi.Helpers
{
    public static class AuditMessageHelper
    {
        public static string FormatMessage(
            string template,
            string? username,
            string module,
            string action,
            int? targetId = null,
            int? cabinetId = null,  
            string? filters = null)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string cabinetName = "";

            var cabinetNames = new Dictionary<int, string>
            {
                {1, "Invoice"},
                {2, "HR"},
                {3, "Statement"},
                {4, "AP Files"},
                {5,"Purchase Orders" }
            };
            if (module == "Document")
            {
                if (cabinetId.HasValue)
            {
                cabinetName = cabinetNames.ContainsKey(cabinetId.Value)
                    ? cabinetNames[cabinetId.Value]
                    : $"Cabinet {cabinetId.Value}";
            }
            }

            return template
                .Replace("{module}", module)
                .Replace("{username}", username)
                .Replace("{action}", action)
                .Replace("{cabinetName}", cabinetName)
                .Replace("{targetId}", targetId?.ToString() ?? "N/A")
                .Replace("{filters}", string.IsNullOrEmpty(filters) ? string.Empty : filters);
        }
    }
}
