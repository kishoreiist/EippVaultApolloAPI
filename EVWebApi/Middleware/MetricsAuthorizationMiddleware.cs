namespace EVWebApi.Middleware
{
    public class MetricsAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HashSet<string> _allowedIps;

        public MetricsAuthorizationMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _allowedIps = config
                .GetSection("Allowed:Prometheus")
                .Get<string[]>()?
                .ToHashSet() ?? new HashSet<string>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/metrics"))
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();

                if (ip == null || !_allowedIps.Contains(ip))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            await _next(context);
        }
    }
}
