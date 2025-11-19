using EVWebApi.DTOs;

namespace EVWebApi.Services
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupDto>> GetAllAsync();
        Task<GroupDto> GetByIdAsync(int id);
        Task<GroupDto> CreateAsync(GroupDto dto);
        Task<GroupDto> UpdateAsync(GroupDto dto);
        Task DeleteAsync(int id);
    }
}
