using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Models;

namespace EVWebApi.Interfaces.Services
{
    public interface IGroupService
    {
        Task<PagedResponse<GroupDto>> GetAllAsync(GroupQueryParameters query);
        Task<GroupDto> GetByIdAsync(int id);
        Task<GroupDto> CreateAsync(CreateGroupDto dto);
        Task<GroupDto> UpdateAsync(UpdateGroupDto dto);
        Task DeleteAsync(int id);
        Task<List<GroupListDto>> GetGroupsForDropdownAsync();

        Task<(byte[], string)> GroupsExportToExcel(GroupQueryParameters query);

        //------------------email grp-------------------------

        Task<EmailGroupDto> CreateEmailGroupAsync(CreateEmailGroupDto dto);
        Task<List<EmailGroup>> GetallEmailGroupForDropDownAsync();
        Task<EmailGroupDto> UpdateEmailGroupAsync(EmailGroupDto emailGroup);
        Task DeleteEmailGroupAsync(int id);
    }
}
