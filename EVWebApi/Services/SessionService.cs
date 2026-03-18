using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Models.Security;

namespace EVWebApi.Services
{
    public class SessionService: ISessionService
    {
        private readonly IUserSessionRepository _repo;
        private readonly IHttpContextAccessor _http;
        public SessionService(IUserSessionRepository repo, IHttpContextAccessor http)
        {
            _repo = repo;
            _http = http;
        }

        public async Task<UserSession> CreateLoginSessionAsync(User user, string refreshTokenHash)
        {

            // revoke previous session
            await _repo.RevokeAllAsync(user.UserId);

            var context = _http.HttpContext;
            if (context == null) return null;

            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = user.UserId,
                JwtId = Guid.NewGuid(),
                RefreshTokenHash = refreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                //IsRevoked = false,
                RevokedAt =null,
                IpAddress = RequestInfoHelper.GetIp(context),
                DeviceInfo = RequestInfoHelper.GetDevice(context)

            };

            await _repo.CreateAsync(session);
            return session;
        }
        public async Task LogoutAsync(int userId, Guid jwtId)
        {
            var session = await _repo.GetByJwtIdAsync(userId, jwtId);
            if (session == null) return;

            //session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(session);
        }

        public async Task LogoutAllAsync(int userId)
        {
            await _repo.RevokeAllAsync(userId);
        }



    }
}
