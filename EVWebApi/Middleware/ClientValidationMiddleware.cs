namespace EVWebApi.Middleware
{
    public class ClientValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _allowedClient;

        public ClientValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _allowedClient = configuration["Security:ClientKey"] ?? "EVWEB";
        }

        public async Task Invoke(HttpContext context)
        {
            // Skip validation for metrics endpoint
            if (context.Request.Path.StartsWithSegments("/metrics"))
            {
                await _next(context);
                return;
            }
            var client = context.Request.Headers["X-App-Client"].ToString();

            if (string.IsNullOrWhiteSpace(client) || client != _allowedClient)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid client");
                return;
            }

            await _next(context);
        }
    

}
}
