using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Models.Security;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EVWebApi.Services
{
    public class SecurityFailureService : ISecurityFailureService
    {


        private readonly AppDbContext _context;
        private const int DailyLimit = 3;
        private const int WeeklyLimit = 6;

        private const int IpDailyLimit = 5;
        private const int IpWeeklyLimit = 10;

        private const int IpTempDaily = 1;
        private const int IpTempWeekly = 2;

        private const string GlobalEndpoint = "GLOBAL_AUTH";
        private const int GlobalLimit = 5;

        public SecurityFailureService(AppDbContext context)
        {
            _context = context;


        }

        public async Task RegisterFailureAsync(int? userId, string? ip, string endpoint)
        {
            //if (!userId.HasValue)
            //    return;

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;

            try
            {
                if (userId.HasValue)
                {
                    await HandleUserFailure(userId.Value, endpoint, ip, now);
                    await HandleUserFailure(userId.Value, GlobalEndpoint, ip, now);
                }
                //else
                //{
                await HandleIpFailure(ip, now);
                //}


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task HandleUserFailure(int userId, string endpoint, string? ip, DateTime now)
        {
            var record = await _context.UserFailureRecords
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Endpoint == endpoint &&
                    x.IsActive);

            if (record == null)
            {
                record = new UserFailureRecord
                {
                    UserId = userId,
                    Endpoint = endpoint,
                    DailyFailures = 0,
                    WeeklyFailures = 0,
                    ValidUpto = now.AddDays(7),
                    CreatedAt = now,
                    LastFailedAt = now,
                    IpAddress = ip,
                    IsActive = true
                };

                _context.UserFailureRecords.Add(record);
            }

            // Reset daily if new day
            if (record.LastFailedAt.Date != now.Date)
                record.DailyFailures = 0;

            // Reset weekly if expired
            if (record.ValidUpto < now)
            {
                record.WeeklyFailures = 0;
                record.ValidUpto = now.AddDays(7);
            }

            record.DailyFailures++;
            record.WeeklyFailures++;
            record.LastFailedAt = now;

            if (endpoint != GlobalEndpoint)
            {
                // DAILY TEMP LOCK
                if (record.DailyFailures >= DailyLimit)
                {
                    await ApplyUserLock(userId, endpoint, isTemporary: true, ip, now);
                    record.IsActive = false;
                    return;
                }

                // WEEKLY PERMANENT LOCK
                if (record.WeeklyFailures >= WeeklyLimit)
                {
                    await ApplyUserLock(userId, endpoint, isTemporary: false, ip, now);
                    record.IsActive = false;
                }
            }
            else
            {
                if (record.DailyFailures >= GlobalLimit)
                {
                    await ApplyUserLock(userId, endpoint, isTemporary: true, ip, now);
                    record.IsActive = false;
                    return;

                }
            }
        }

        private async Task ApplyUserLock(
            int userId,
            string endpoint,
            bool isTemporary,
            string? ip,
            DateTime now)
        {
            var activeLock = await _context.LockAudit
            .Where(x => x.UserId == userId && x.UnlockedBy == null)
            .OrderByDescending(x => x.LockedAt)
            .FirstOrDefaultAsync();
            if (activeLock != null)
            {
                // If permanent lock → never override
                if (activeLock.LockedUntil == null)
                    return;

                // If temporary/global lock still active → do not stack
                if (activeLock.LockedUntil > now)
                    return;

                // If expired → auto unlock
                activeLock.UnlockedAt = now;
                activeLock.UnlockedBy = "SYSTEM_AUTO_UNLOCK";
            }
            string lockType = endpoint == GlobalEndpoint
                ? "GLOBAL"
                : isTemporary ? "TEMPORARY" : "PERMANENT";

            var audit = new AccountLockAudit
            {
                UserId = userId,
                LockType = lockType,
                Reason = isTemporary
                    ? $"Daily limit exceeded for {endpoint}"
                    : $"Weekly limit exceeded for {endpoint}",
                LockedAt = now,
                LockedUntil = isTemporary ? now.AddHours(24) : null
            };

            _context.LockAudit.Add(audit);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = UserStatus.Locked;
            }

            // If permanent lock & IP neeed to be blacklisted--->this block is executed only for user-based locks, not for IP-based locks
            if (!isTemporary && !string.IsNullOrEmpty(ip))
            {
                await HandlePermanentLockForIp(ip, userId, now);
            }
            await _context.SaveChangesAsync();
        }

        private async Task HandlePermanentLockForIp(string ip, int? userId, DateTime now)
        {
            var ipState = await _context.IpSecurity
                .FirstOrDefaultAsync(x => x.IpAddress == ip);

            if (ipState == null)
            {
                ipState = new IpSecurityState
                {
                    IpAddress = ip,
                    IPDailyFailures = 0,
                    IPWeeklyFailures = 0,
                    ValidUpto = now.AddDays(7),
                    LastActivityAt = now
                };

                _context.IpSecurity.Add(ipState);
            }

            if (ipState.LastActivityAt.Date != now.Date)
                ipState.IPDailyFailures = 0;

            if (ipState.ValidUpto < now)
            {
                ipState.IPWeeklyFailures = 0;
                ipState.ValidUpto = now.AddDays(7);
            }

            ipState.IPDailyFailures++;
            ipState.IPWeeklyFailures++;
            ipState.LastActivityAt = now;

            // ONLY log permanent lock if userId exists
            if (userId.HasValue)
            {
                _context.IpPermanentLockLogs.Add(new IpPermanentLockLog
                {
                    IpAddress = ip,
                    UserId = userId.Value,
                    CreatedAt = now
                });
            }

            // Count only NON-NULL users--how many users have locked by this ip in last 30 days
            //var distinctUsers = await _context.IpPermanentLockLogs
            //    .Where(x => x.IpAddress == ip &&
            //                x.UserId != null &&
            //                x.CreatedAt >= now.AddDays(-30))
            //    .Select(x => x.UserId)
            //    .Distinct()
            //    .CountAsync();

            //ipState.PermanentLockCount = distinctUsers;

            //if (distinctUsers >= 2 ||----------need to check
            if(ipState.IPWeeklyFailures >= IpTempWeekly)
            {
                ipState.Status = IpSecurityStatus.Blacklisted;
                ipState.BlacklistedAt ??= now;
                ipState.ValidUpto = null;
            }
            else if (ipState.IPDailyFailures >= IpTempDaily)
            {
                ipState.Status = IpSecurityStatus.TempBlacklisted;
                ipState.BlacklistedAt ??= now;
                ipState.ValidUpto = now.AddHours(24);
                
            }

            else
            {
                ipState.Status = IpSecurityStatus.Warning;//temp blklist 
            }
        }

        private async Task HandleIpFailure(string ip, DateTime now)
        {
            var record = await _context.IpFailureRecords
                .FirstOrDefaultAsync(x => x.IpAddress == ip);

            if (record == null)
            {
                record = new IpFailureRecord
                {
                    IpAddress = ip,
                    DailyFailures = 0,
                    WeeklyFailures = 0,
                    ValidUpto = now.AddDays(7),
                    LastFailedAt = now
                };

                _context.IpFailureRecords.Add(record);
            }

            if (record.LastFailedAt.Date != now.Date)
                record.DailyFailures = 0;

            if (record.ValidUpto < now)
            {
                record.WeeklyFailures = 0;
                record.ValidUpto = now.AddDays(7);
            }

            record.DailyFailures++;
            record.WeeklyFailures++;
            record.LastFailedAt = now;

            if (record.DailyFailures >= IpDailyLimit ||
                record.WeeklyFailures >= IpWeeklyLimit)
            {
                await HandlePermanentLockForIp(ip, null, now);
            }
        }

        public async Task<bool> IsIpBlacklistedAsync(string ip)
        {
            var ipState = await _context.IpSecurity
                //.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IpAddress == ip);
            if (ipState == null)
                return false;
            var now = DateTime.UtcNow;

            if (ipState.Status == IpSecurityStatus.Blacklisted)
                return true;

            if (ipState.Status == IpSecurityStatus.TempBlacklisted)
            {
                if (ipState.BlacklistedAt.HasValue && ipState.ValidUpto.Value <= now)
                {
                    ipState.Status = IpSecurityStatus.Warning;
                    ipState.BlacklistedAt = null;
                    await _context.SaveChangesAsync();
                    return false;
                }
                return true;
            }
            return false;
        }

        public async Task<bool> IsUserLockedAsync(int userId)//need to optimize
        {
            var user = await _context.Users
                //.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null) return false;

            if (user?.Status != UserStatus.New && user?.Status != UserStatus.Locked)// If status is NOT New AND NOT Locked → return false
                return false;
            var now = DateTime.UtcNow;
            var activeLock = await _context.LockAudit
            .Where(x => x.UserId == userId && x.UnlockedBy == null)
            .OrderByDescending(x => x.LockedAt)
            .FirstOrDefaultAsync();

            if (activeLock == null && user.Status==UserStatus.Locked)
            {
                // Safety: no active lock record but status is Locked
                user.Status = UserStatus.Active;
                await _context.SaveChangesAsync();
                return false;
            }
            if (activeLock.LockedUntil == null)// for enhancing permnt lock will only be done by admin
                return true;
            if (activeLock.LockedUntil <= now)
            {
                activeLock.UnlockedAt = now;
                activeLock.UnlockedBy = "SYSTEM_AUTO_UNLOCK";

                await _context.SaveChangesAsync();
                return false;
            }
            return true;
        }


    }
    
}
