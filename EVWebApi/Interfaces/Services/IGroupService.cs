using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;

namespace EVWebApi.Interfaces.Services
{
    public interface IGroupService
    {
        Task<PagedResponse<GroupDto>> GetAllAsync(GroupQueryParameters query);
        Task<GroupDto> GetByIdAsync(int id);
        Task<GroupDto> CreateAsync(GroupDto dto);
        Task<GroupDto> UpdateAsync(GroupDto dto);
        Task DeleteAsync(int id);
    }
}
