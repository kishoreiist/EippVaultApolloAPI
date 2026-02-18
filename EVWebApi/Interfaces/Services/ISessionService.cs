using EVWebApi.Models;
using EVWebApi.Models.Security;

namespace EVWebApi.Interfaces.Services
{
    public interface ISessionService
    {
        Task<UserSession> CreateLoginSessionAsync(User user);
        Task LogoutAsync(int userId, Guid jwtId);
        Task LogoutAllAsync(int userId);
    }
}
