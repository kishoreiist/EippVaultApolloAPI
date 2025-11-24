using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;

namespace EVWebApi.Interfaces.Services
{
    public interface IUserService
    {
        Task<PagedResponse<UserDto>> GetAllAsync(UserQueryParameters query);
        Task<UserDto> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto> UpdateAsync(UpdateUserDto dto);
        Task DeleteAsync(int id);
    }
}
