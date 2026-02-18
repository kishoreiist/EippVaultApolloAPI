using EVWebApi.Models.Security;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IUserSessionRepository
    {
        Task<UserSession?> GetActiveSessionAsync(int userId);
        Task<UserSession?> GetByIdAsync(Guid sessionId);
        Task CreateAsync(UserSession session);
        Task UpdateAsync(UserSession session);
        Task RevokeAsync(Guid sessionId);
        Task RevokeAllAsync(int userId);
        Task<UserSession?> GetByJwtIdAsync(int userId, Guid jwtId);
    }
}
