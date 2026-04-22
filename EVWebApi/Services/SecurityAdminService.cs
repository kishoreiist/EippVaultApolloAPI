using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.Security;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Helpers.ExportToExcel;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Models.Security;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.InteractiveChat;
using System.Linq.Dynamic.Core;

namespace EVWebApi.Services
{
    public class SecurityAdminService : ISecurityAdminService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepo;
        private readonly IAuthService _authService;
        public SecurityAdminService(AppDbContext context, IMapper mapper, IUserRepository userRepo, IAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            _userRepo = userRepo;
            _authService = authService;
        }
        public async Task<PagedResponse<BlacklistDto>> GetBlacklistedIpsAsync(BlacklistQueryParameters query)
        {
            //var ips = _context.IpSecurity
            //     .Where(x => x.Status != IpSecurityStatus.Normal)
            //     // .OrderByDescending(x => x.BlacklistedAt)
            //     .AsNoTracking().AsQueryable();

            //// IP Address Filter
            //if (!string.IsNullOrWhiteSpace(query.IpAddress))
            //{
            //    ips = ips.Where(x =>
            //        EF.Functions.Like(x.IpAddress, $"%{query.IpAddress}%"));
            //}

            //// Status Filter
            //if (query.Status.HasValue)
            //{
            //    ips = ips.Where(x => x.Status == query.Status.Value);
            //}
            //else
            //{
            //    ips = ips.Where(x => x.Status != IpSecurityStatus.Normal && (x.ValidUpto == null || x.ValidUpto > DateTime.UtcNow));
            //}

            ////Blacklisted Date Range
            //if (query.BlacklistedFrom.HasValue)
            //{
            //    ips = ips.Where(x => x.BlacklistedAt >= query.BlacklistedFrom.Value);
            //}

            //if (query.BlacklistedTo.HasValue)
            //{
            //    var endOfDay = query.BlacklistedTo.Value.Date.AddDays(1).AddTicks(-1);
            //    ips = ips.Where(x => x.BlacklistedAt <= endOfDay);
            //}

            //// Last Activity Date Range
            //if (query.LastActivityFrom.HasValue)
            //{
            //    ips = ips.Where(x => x.LastActivityAt >= query.LastActivityFrom.Value);
            //}

            //if (query.LastActivityTo.HasValue)
            //{
            //    var endOfDay = query.LastActivityTo.Value.Date.AddDays(1).AddTicks(-1);
            //    ips = ips.Where(x => x.LastActivityAt <= endOfDay);
            //}

            //Order
            var ips= ApplyIPStatusFilters(query);
            ips=ips.AsNoTracking().AsQueryable()               
                .OrderByDescending(x => x.BlacklistedAt);



            var pagedResult = await ips
                .ProjectTo<BlacklistDto>(_mapper.ConfigurationProvider)
                .GetPagedResponseAsync(query.PageNumber, query.PageSize);
            return pagedResult;
        }

        public async Task<PagedResponse<LockedDto>> GetLockedUsersAsync(LockedUserQueryParameters query)
        {
            //var users = _context.LockAudit.AsNoTracking().AsQueryable();
            var users = ApplyLockedUserFilters(query);
            ////filter by name
            //if (!string.IsNullOrWhiteSpace(query.Name))
            //{
            //    var pattern = $"%{query.Name}%";

            //    users = users.Where(x =>
            //        EF.Functions.ILike(
            //            x.User.FirstName + " " + x.User.LastName,
            //            pattern));
            //}

            //// Filter by LockType
            //if (!string.IsNullOrWhiteSpace(query.LockType))
            //{
            //    users = users.Where(x =>
            //        EF.Functions.ILike(x.LockType, $"%{query.LockType}%"));
            //}

            //// Filter by Reason
            //if (!string.IsNullOrWhiteSpace(query.Reason))
            //{
            //    users = users.Where(x =>
            //        EF.Functions.ILike(x.Reason, $"%{query.Reason}%"));
            //}

            ////filter by lock date range
            //if (query.LockedFrom.HasValue)
            //{
            //    users = users.Where(x => x.LockedAt >= query.LockedFrom.Value);
            //}

            //if (query.LockedTo.HasValue)
            //{
            //    var endOfDay = query.LockedTo.Value.Date.AddDays(1).AddTicks(-1);
            //    users = users.Where(x => x.LockedAt <= endOfDay);
            //}

            ////status
            //if (!string.IsNullOrEmpty(query.Status))
            //{
            //    switch (query.Status.ToLower())
            //    {
            //        case "active":
            //            users = users.Where(x =>
            //                x.UnlockedAt == null &&
            //                (x.LockedUntil == null || x.LockedUntil > DateTime.UtcNow));
            //            break;

            //        case "unlocked":
            //            users = users.Where(x => x.UnlockedAt != null);
            //            break;

            //        case "all":
            //        default:
            //            break;
            //    }
            //}
            //else
            //{
            //    // default → active only
            //    users = users.Where(x =>
            //        x.UnlockedAt == null &&
            //        (x.LockedUntil == null || x.LockedUntil > DateTime.UtcNow));
            //}

            users = users.OrderByDescending(x => x.LockedAt);

            var result = await users
                .ProjectTo<LockedDto>(_mapper.ConfigurationProvider)
                .GetPagedResponseAsync(query.PageNumber, query.PageSize);
            return result;
        }


        public async Task<bool> UnlockUserAsync(int userId, int? currentuserid)
        {
            //var user = await _userRepo.GetByIdAsync(userId);
            var user = await _userRepo.GetByIdIncludingLockedAsync(userId);
            if (user == null || user.Status != UserStatus.Locked)
                throw new NotFoundException($"No existing lock for this user.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lockEntry = await _context.LockAudit
                .Where(x => x.UserId == userId && x.LockedUntil == null)//only selects permnt locks
                .OrderByDescending(x => x.LockedAt)
                .FirstOrDefaultAsync();

                if (lockEntry == null || lockEntry.UnlockedAt != null)
                    return false; // No active lock found
                lockEntry.UnlockedAt = DateTime.UtcNow;
                lockEntry.UnlockedBy = $"Admin - {currentuserid}";
                lockEntry.LockedUntil = DateTime.UtcNow;
                user.Status = UserStatus.New;
                await _context.SaveChangesAsync();
                await _authService.PasswordResetSendEmailAsync(user, Enums.PasswordEmailType.AccountLocked);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Unlock failed: Could not send reset email. Changes rolled back.", ex);
            }
        }

        public async Task<string> RemoveBlackListIpAsync(int id, int? currentuserid)
        {
            var ipState = await _context.IpSecurity
                .Where(x => x.Status == IpSecurityStatus.Blacklisted)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ipState == null)
                throw new NotFoundException($"No existing lock for this ip address.");

            ipState.Status = IpSecurityStatus.Normal;
            ipState.ValidUpto = DateTime.UtcNow;
            ipState.UnlockedAt = DateTime.UtcNow;
            ipState.UnlockedBy = $"Admin - {currentuserid}";

            await _context.SaveChangesAsync();
            return ipState.IpAddress;
        }



        public async Task<(byte[], string)> LockedUsersExportToExcel(LockedUserQueryParameters query)
        {

            // force no pagination
            query.PageNumber = 1;
            query.PageSize = int.MaxValue;

            var usersQuery = ApplyLockedUserFilters(query);
            var users = await usersQuery
                .AsNoTracking()
                .ToListAsync();
            var columns = new List<string>
            {
                "Name",
                "Reason",
                "LockType",
                "LockedTime"

            };

            var excel = ExportExcelBuildHelper.BuildExcel(
                users,
                columns,
                (u, col) => ExcelColumnsHelper.GetLockedUserColumnValue(u, col)
            );

            return (excel, $"LockedUsers__{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx");
        }

        public async Task<(byte[], string)> IPStatusExportToExcel(BlacklistQueryParameters query)
        {

            // force no pagination
            query.PageNumber = 1;
            query.PageSize = int.MaxValue;

            var ipQuery = ApplyIPStatusFilters(query);
            var ips = await ipQuery
                .AsNoTracking()
                .ToListAsync();
            var columns = new List<string>
            {
                "IPAddress",
                "Status",
                "DailyFailures",
                "WeeklyFailures",
                "BlackListedTime",
                "LastActivityTime"

            };

            var excel = ExportExcelBuildHelper.BuildExcel(
                ips,
                columns,
                (u, col) => ExcelColumnsHelper.GetIPStatusColumnValue(u, col)
            );

            return (excel, $"IPStatus__{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx");
        }

        private IQueryable<AccountLockAudit> ApplyLockedUserFilters(LockedUserQueryParameters query)
        {
            var users = _context.LockAudit
                 .Include(x => x.User).AsNoTracking().AsQueryable();

            //filter by name
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                var pattern = $"%{query.Name}%";

                users = users.Where(x =>
                    EF.Functions.ILike(
                        x.User.FirstName + " " + x.User.LastName,
                        pattern));
            }

            // Filter by LockType
            if (!string.IsNullOrWhiteSpace(query.LockType))
            {
                users = users.Where(x =>
                    EF.Functions.ILike(x.LockType, $"%{query.LockType}%"));
            }

            // Filter by Reason
            if (!string.IsNullOrWhiteSpace(query.Reason))
            {
                users = users.Where(x =>
                    EF.Functions.ILike(x.Reason, $"%{query.Reason}%"));
            }

            //filter by lock date range
            if (query.LockedFrom.HasValue)
            {
                users = users.Where(x => x.LockedAt >= query.LockedFrom.Value);
            }

            if (query.LockedTo.HasValue)
            {
                var endOfDay = query.LockedTo.Value.Date.AddDays(1).AddTicks(-1);
                users = users.Where(x => x.LockedAt <= endOfDay);
            }

            //status
            if (!string.IsNullOrEmpty(query.Status))
            {
                switch (query.Status.ToLower())
                {
                    case "active":
                        users = users.Where(x =>
                            x.UnlockedAt == null &&
                            (x.LockedUntil == null || x.LockedUntil > DateTime.UtcNow));
                        break;

                    case "unlocked":
                        users = users.Where(x => x.UnlockedAt != null);
                        break;

                    case "all":
                    default:
                        break;
                }
            }
            else
            {
                // default → active only
                users = users.Where(x =>
                    x.UnlockedAt == null &&
                    (x.LockedUntil == null || x.LockedUntil > DateTime.UtcNow));
            }
            return users;

        }

        private IQueryable<IpSecurityState> ApplyIPStatusFilters(BlacklistQueryParameters query)
        {
            var ips = _context.IpSecurity
                 .Where(x => x.Status != IpSecurityStatus.Normal)            
                  .AsNoTracking().AsQueryable();

            // IP Address Filter
            if (!string.IsNullOrWhiteSpace(query.IpAddress))
            {
                ips = ips.Where(x =>
                    EF.Functions.Like(x.IpAddress, $"%{query.IpAddress}%"));
            }

            // Status Filter
            if (query.Status.HasValue)
            {
                ips = ips.Where(x => x.Status == query.Status.Value);
            }
            else
            {
                ips = ips.Where(x => x.Status != IpSecurityStatus.Normal && (x.ValidUpto == null || x.ValidUpto > DateTime.UtcNow));
            }

            //Blacklisted Date Range
            if (query.BlacklistedFrom.HasValue)
            {
                ips = ips.Where(x => x.BlacklistedAt >= query.BlacklistedFrom.Value);
            }

            if (query.BlacklistedTo.HasValue)
            {
                var endOfDay = query.BlacklistedTo.Value.Date.AddDays(1).AddTicks(-1);
                ips = ips.Where(x => x.BlacklistedAt <= endOfDay);
            }

            // Last Activity Date Range
            if (query.LastActivityFrom.HasValue)
            {
                ips = ips.Where(x => x.LastActivityAt >= query.LastActivityFrom.Value);
            }

            if (query.LastActivityTo.HasValue)
            {
                var endOfDay = query.LastActivityTo.Value.Date.AddDays(1).AddTicks(-1);
                ips = ips.Where(x => x.LastActivityAt <= endOfDay);
            }
            return ips;
        }
    }
}

