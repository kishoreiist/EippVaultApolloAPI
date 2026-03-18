namespace EVWebApi.Helpers
{
    public static class RequestInfoHelper
    {
        public static string? GetIp(HttpContext context)
        {
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(ip)
                ? context.Connection.RemoteIpAddress?.ToString()
                : ip;
        }

        public static string? GetDevice(HttpContext context)
        {
            return context?.Request.Headers.UserAgent.ToString();
        }
    }
}
