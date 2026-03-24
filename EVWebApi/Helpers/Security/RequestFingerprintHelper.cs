namespace EVWebApi.Helpers.Security
{
    public static class RequestFingerprintHelper
    {
        public static string GetFingerprint(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ua = context.Request.Headers.UserAgent.ToString();
            var lang = context.Request.Headers.AcceptLanguage.ToString();

            return $"{ip}-{ua}-{lang}".ToLower();
        }
    }
}
