using EVWebApi.Helpers;
using EVWebApi.Settings;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace EVWebApi.Middleware
{
    public class RefreshRateLimitPolicy : IRateLimiterPolicy<string>
    {
        private readonly IMemoryCache _cache;
        private readonly AllowedIpSettings _allowedIpSettings;

        public RefreshRateLimitPolicy(
            IMemoryCache cache,
            IOptions<AllowedIpSettings> options)
        {
            _cache = cache;
            _allowedIpSettings = options.Value;
        }

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 1️⃣ If no token → limit by IP
            if (string.IsNullOrEmpty(refreshToken) || refreshToken.Length != 88)
            {
                return CreatePartition($"ip:{ip}", 5);
            }

            var tokenHash = RefreshTokenHelper.HashToken(refreshToken);

            // 2️⃣ If token already mapped to user in cache
            if (_cache.TryGetValue(tokenHash, out int userId))
            {
                return CreatePartition($"user:{userId}-ip:{ip}", 5);
            }

            // 3️⃣ Whitelisted IP bypass
            if (_allowedIpSettings.AllowedIPs.Contains(ip))
            {
                return RateLimitPartition.GetNoLimiter($"whitelisted:{ip}");
            }

            // 4️⃣ Unknown token → limit by IP
            return CreatePartition($"ip:{ip}", 5);
        }

        private RateLimitPartition<string> CreatePartition(string key, int limit)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
            async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsync(
                    "Too many refresh attempts. Try again after sometime",
                    token);
            };
    }
}