using EVWebApi.DTOs.HR;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocAccessReqService
    {
        Task RequestAccessAsync(int userId, AccessRequestDto dto);
        Task HandleAccessRequestAsync(int adminId, AccessActionDto dto);
    }
}
