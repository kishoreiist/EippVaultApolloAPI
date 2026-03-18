using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.Security;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
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
        public SecurityAdminService(AppDbContext context, IMapper mapper,IUserRepository userRepo,IAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            _userRepo = userRepo;
            _authService = authService;
        }
        public async Task<PagedResponse<BlacklistDto>> GetBlacklistedIpsAsync(BlacklistQueryParameters query)
        {
            var ips = _context.IpSecurity
                 .Where(x => x.Status!=IpSecurityStatus.Normal)
                 // .OrderByDescending(x => x.BlacklistedAt)
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

            //Order
            ips = ips.OrderByDescending(x => x.BlacklistedAt);


            var pagedResult = await ips
                .ProjectTo<BlacklistDto>(_mapper.ConfigurationProvider)
                .GetPagedResponseAsync(query.PageNumber, query.PageSize);
            return pagedResult;
        }

        public async Task<PagedResponse<LockedDto>> GetLockedUsersAsync(LockedUserQueryParameters query)
        {
            var users = _context.LockAudit.AsNoTracking().AsQueryable();

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

            users = users.OrderByDescending(x => x.LockedAt);

            var result = await users
                .ProjectTo<LockedDto>(_mapper.ConfigurationProvider)
                .GetPagedResponseAsync(query.PageNumber, query.PageSize);
            return result;
        }


        public async Task<bool> UnlockUserAsync(int userId,int? currentuserid)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || user.Status!=UserStatus.Locked)
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
                await _authService.PasswordResetSendEmailAsync(user);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex) 
            {
                await transaction.RollbackAsync();
                throw new Exception("Unlock failed: Could not send reset email. Changes rolled back.", ex);
            }
        }

        public async Task RemoveBlackListIpAsync(string ip)
        {
            var ipState = await _context.IpSecurity
                .Where(x=>x.Status==IpSecurityStatus.Blacklisted)
                .FirstOrDefaultAsync(x => x.IpAddress == ip);

            if (ipState == null)
                return;

            ipState.Status = IpSecurityStatus.Normal;
            ipState.ValidUpto = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}

