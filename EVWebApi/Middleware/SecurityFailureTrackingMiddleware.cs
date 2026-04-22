using DocumentFormat.OpenXml.InkML;
using EVWebApi.Data;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Settings;
using EVWebAPI.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EVWebApi.Middleware
{

    public class SecurityFailureTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AllowedIpSettings _allowedIpSettings;
        private readonly ILogger<SecurityFailureTrackingMiddleware> _logger;
        public SecurityFailureTrackingMiddleware(RequestDelegate next, IOptions<AllowedIpSettings> options, ILogger<SecurityFailureTrackingMiddleware> logger)
        {
            _next = next;
            _allowedIpSettings = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(
        HttpContext context,
        ISecurityFailureService failureService,
        AppDbContext db)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (!_allowedIpSettings.AllowedIPs.Contains(ip))
            {
                _logger.LogWarning("Unauthorized IP access attempt: {IP}", ip);
            }


            bool isWhitelisted = _allowedIpSettings.AllowedIPs.Contains(ip);
            // Block Blacklisted IP from attempting
            if (!isWhitelisted)
            {
                if (!string.IsNullOrEmpty(ip) &&
                    await failureService.IsIpBlacklistedAsync(ip))
                {

                    throw new IpBlacklistedException("IP is blacklisted.");

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

                    throw;
                }

                await failureService.RegisterFailureAsync(failedUserId, ip, endpoint); 
                throw;
            }


            catch(AuthenticationException ex)
            {


                    failedUserId = GetUserIdFromContext(context);

                    await failureService.RegisterFailureAsync(failedUserId, ip, endpoint);
              
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