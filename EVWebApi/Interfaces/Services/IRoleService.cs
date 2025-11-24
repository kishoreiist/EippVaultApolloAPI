using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.Role;

namespace EVWebApi.Interfaces.Services
{
    public interface IRoleService
    {
        Task<PagedResponse<RoleDto>> GetAllAsync(RoleQueryParameters query);
        Task<RoleDto> GetByIdAsync(int id);
        Task<RoleDto> CreateAsync(CreateRoleDto dto);
        Task<RoleDto> UpdateAsync(UpdateRoleDto dto);
        Task DeleteAsync(int id);
    }
}

