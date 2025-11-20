using EVWebApi.DTOs;

namespace EVWebApi.Interfaces.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllAsync();
        Task<RoleDto> GetByIdAsync(int id);
        Task<RoleDto> CreateAsync(CreateRoleDto dto);
        Task<RoleDto> UpdateAsync(UpdateRoleDto dto);
        Task DeleteAsync(int id);
    }
}

