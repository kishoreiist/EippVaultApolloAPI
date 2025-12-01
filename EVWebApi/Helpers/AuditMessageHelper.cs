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
            string? filters = null)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            return template
                .Replace("{module}", module)
                .Replace("{username}",username)
                .Replace("{action}", action)
                .Replace("{targetId}", targetId?.ToString() ?? "N/A")
                .Replace("{filters}", string.IsNullOrEmpty(filters) ? string.Empty : filters);
        }
    }
}