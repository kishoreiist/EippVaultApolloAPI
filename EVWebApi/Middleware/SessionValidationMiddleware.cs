using EVWebApi.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace EVWebApi.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserSessionRepository repo)
        {
            if (context.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var sessionIdClaim = context.User.FindFirst("session_id");

                if (sessionIdClaim == null)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Session missing");
                    return;
                }

                if (sessionIdClaim != null &&
                    Guid.TryParse(sessionIdClaim.Value, out var sessionId))
                {
                    var session = await repo.GetByIdAsync(sessionId);

                    if (session == null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Session expired");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
