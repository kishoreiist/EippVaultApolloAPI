using EVWebAPI.Controllers;

namespace EVWebApi.Middleware
{
    public class BotDetectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BotDetectionMiddleware> _logger;
        private static readonly string[] BlockedAgents =
        {
        "curl",
        "wget",
        "python",
        "scrapy",
        "postman",
        "insomnia",
        "bot",
        "crawler",
        "spider",
        "selenium",
        "headless",
        "phantom"
    };

        public BotDetectionMiddleware(RequestDelegate next, ILogger<BotDetectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

            if (string.IsNullOrEmpty(userAgent) ||
                BlockedAgents.Any(b => userAgent.Contains(b)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                _logger.LogWarning("Bot blocked: {UserAgent}", userAgent);
                await context.Response.WriteAsync("Bot traffic blocked");
                return;
            }

            await _next(context);
        }
    }
}
