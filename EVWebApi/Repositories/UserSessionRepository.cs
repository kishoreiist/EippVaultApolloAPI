using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class UserSessionRepository :IUserSessionRepository
    {
        private readonly AppDbContext _context;

        public UserSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        //get active session for login check
        public async Task<UserSession?> GetActiveSessionAsync(int userId)
        {
            return await _context.UserSessions
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsRevoked &&
                    x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        //get session by id (used by middleware)
        public async Task<UserSession?> GetByIdAsync(Guid sessionId)
        {
            return await _context.UserSessions
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);
        }

        // create new session
        public async Task CreateAsync(UserSession session)
        {
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // revoke one session (logout)
        public async Task RevokeAsync(Guid sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);

            if (session == null) return;

            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // revoke all sessions (single-login enforcement)
        public async Task RevokeAllAsync(int userId)
        {
            var sessions = await _context.UserSessions
                .Where(x =>
                    x.UserId == userId &&
                    (x.IsRevoked==false))
                .ToListAsync();

            if (!sessions.Any()) return;

            foreach (var s in sessions)
            {
                s.IsRevoked = true;
                s.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserSession?> GetByJwtIdAsync(int userId, Guid jwtId)
        {
            return await _context.UserSessions
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.JwtId == jwtId &&
                    !x.IsRevoked);
        }

        public async Task UpdateAsync(UserSession session)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }


    }
}
