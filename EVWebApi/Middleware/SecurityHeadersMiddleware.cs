namespace EVWebApi.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                headers.XXSSProtection = "1; mode=block"; // For older browsers

                headers.ContentSecurityPolicy =
                    "default-src 'self'; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self';";
                
             return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
