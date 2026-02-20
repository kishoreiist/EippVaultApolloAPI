using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.Security;

namespace EVWebApi.Interfaces.Services
{
    public interface ISecurityAdminService
    {
        Task<PagedResponse<BlacklistDto>> GetBlacklistedIpsAsync(BlacklistQueryParameters query);

        Task<PagedResponse<LockedDto>> GetLockedUsersAsync(LockedUserQueryParameters query);

        Task<bool> UnlockUserAsync(int userId, int? currentuserid);
        Task RemoveBlackListIpAsync(string ip);
    }
}
