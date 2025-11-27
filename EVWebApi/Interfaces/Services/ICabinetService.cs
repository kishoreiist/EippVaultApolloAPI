using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;

namespace EVWebApi.Interfaces.Services
{
    public interface ICabinetService
    {
        Task<PagedResponse<CabinetDto>> GetAllAsync(CabinetQueryParameters query);
        Task<CabinetDto> GetByIdAsync(int id);
        Task<CabinetDto> CreateAsync(CreateCabinetDto dto);
        Task<CabinetDto> UpdateAsync(UpdateCabinetDto dto);
        Task DeleteAsync(int id);
    }
}
