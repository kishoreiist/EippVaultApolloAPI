using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.Security;
using EVWebApi.DTOs.User;

namespace EVWebApi.Interfaces.Services
{
    public interface ISecurityAdminService
    {
        Task<PagedResponse<BlacklistDto>> GetBlacklistedIpsAsync(BlacklistQueryParameters query);

        Task<PagedResponse<LockedDto>> GetLockedUsersAsync(LockedUserQueryParameters query);

        Task<bool> UnlockUserAsync(int userId, int? currentuserid);
        Task<string> RemoveBlackListIpAsync(int id, int? currentuserid);

        Task<(byte[], string)> LockedUsersExportToExcel(LockedUserQueryParameters query);
        Task<(byte[], string)> IPStatusExportToExcel(BlacklistQueryParameters query);
    }
}
