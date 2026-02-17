using DocumentFormat.OpenXml.InkML;
using EVWebApi.Data;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EVWebApi.Middleware
{

    public class SecurityFailureTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AllowedIpSettings _allowedIpSettings;
        public SecurityFailureTrackingMiddleware(RequestDelegate next, IOptions<AllowedIpSettings> options)
        {
            _next = next;
            _allowedIpSettings = options.Value;
        }

        public async Task InvokeAsync(
        HttpContext context,
        ISecurityFailureService failureService,
        AppDbContext db)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            Console.WriteLine($"IP from request: {ip}");
            foreach (var allowed in _allowedIpSettings.AllowedIPs)
            {
                Console.WriteLine($"Allowed IP: '{allowed}'");
            }


            bool isWhitelisted = _allowedIpSettings.AllowedIPs.Contains(ip);
            // Block Blacklisted IP from attempting
            if (!isWhitelisted)
            {
                if (!string.IsNullOrEmpty(ip) &&
                    await failureService.IsIpBlacklistedAsync(ip))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("IP is blacklisted.");
                    return;
                }
            }

            var endpoint = context.Request.Path.Value?.ToLower().TrimEnd('/') ?? string.Empty;
            int? failedUserId = null;

            if (context.User.Identity?.IsAuthenticated == true)
            {
                if (int.TryParse(context.User.FindFirst("userId")?.Value, out var id))
                {
                    failedUserId = id;
                }
            }
            else if (context.Items.TryGetValue("UserId", out var uid))
            {
                failedUserId = (int)uid;
            }



            try
            {

                // Block Locked User
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    if (int.TryParse(context.User.FindFirst("userId")?.Value, out var userId))
                    {
                        var user = await db.Users
                            .FirstOrDefaultAsync(x => x.UserId == userId);

                        if (user != null && user.Status == UserStatus.Locked)
                        {
                            var permanentLock = await db.LockAudit
                             .Where(x => x.UserId == userId && x.LockType == "PERMANENT")
                             .OrderByDescending(x => x.LockedAt)
                             .FirstOrDefaultAsync();

                            if (permanentLock != null)
                            {
                                context.Response.StatusCode = 403;
                                await context.Response.WriteAsync("Account is permanently locked.");
                                return;
                            }
                            var lastTempLock = await db.LockAudit
                                .Where(x => x.UserId == userId && x.LockType == "TEMPORARY")
                                .OrderByDescending(x => x.LockedAt)
                                .FirstOrDefaultAsync();

                            if (lastTempLock?.LockedUntil > DateTime.UtcNow)
                            {
                                context.Response.StatusCode = 403;
                                await context.Response.WriteAsync("Account is temporarily locked.");
                                return;
                            }

                            if (lastTempLock?.LockedUntil < DateTime.UtcNow)
                            {
                                user.Status = UserStatus.Active;
                                await db.SaveChangesAsync();
                            }

                        }
                    }
                }

                await _next(context);

            }
            catch (AuthorizationException ex)
            {
                failedUserId = GetUserIdFromContext(context);
                if (failedUserId.HasValue
                && await failureService.IsUserLockedAsync(failedUserId.Value)
                )
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Account is locked by Admin");
                    return;
                }

                await failureService.RegisterFailureAsync(failedUserId, ip, endpoint); 
                throw;
            }

            catch(AccountNotActivatedException ex)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Account not activated");
                return;
            }
            catch(AccountDeletedException ex)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Account is deleted/disabled");
                return;
            }
            catch (AccountDisabledException ex)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Account is locked by Admin");
                return;
            }
            catch (LockedException ex)
            {

                 context.Response.StatusCode = StatusCodes.Status409Conflict;
                 await context.Response.WriteAsync( "Your request is success,for further clarification check your email");
                return ;
                
            }
            catch(AuthenticationException ex)
            {

                // Track Failures AFTER execution
                //if (context.Response.StatusCode == 401 ||
                //    context.Response.StatusCode == 403)
                //{
                    failedUserId = GetUserIdFromContext(context);

                    await failureService.RegisterFailureAsync(failedUserId, ip, endpoint);
                //}
                throw;
            }
        }
        private int? GetUserIdFromContext(HttpContext context)
        {   int? failedUserId = null;
            if (context.User.Identity?.IsAuthenticated == true)
            {
                if (int.TryParse(context.User.FindFirst("userId")?.Value, out var id))
                {
                    failedUserId = id;
                }
            }
            else if (context.Items.TryGetValue("UserId", out var uid))
            {
                failedUserId = (int)uid;
            }
            return failedUserId;
        }


    }
}