using EVWebApi.DTOs;

namespace EVWebApi.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto> UpdateAsync(UpdateUserDto dto);
        Task DeleteAsync(int id);
    }
}
