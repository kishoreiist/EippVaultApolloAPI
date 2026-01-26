using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;

namespace EVWebApi.Interfaces.Services
{
    public interface IGroupService
    {
        Task<PagedResponse<GroupDto>> GetAllAsync(GroupQueryParameters query);
        Task<GroupDto> GetByIdAsync(int id);
        Task<GroupDto> CreateAsync(CreateGroupDto dto);
        Task<GroupDto> UpdateAsync(UpdateGroupDto dto);
        Task DeleteAsync(int id);
        Task<List<ListDto>> GetGroupsForDropdownAsync();
    }
}
