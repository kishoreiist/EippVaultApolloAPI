namespace EVWebApi.Repositories
{
    using EVWebApi.Data;
    using EVWebApi.Interfaces;
    using Microsoft.EntityFrameworkCore;

    public class MfaRepository : IMfaRepository
    {
        private readonly AppDbContext _context;

        public MfaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveMfaTokenAsync(UserMfaToken token)
        {
            var mfa = await _context.UserMfaTokens.FirstOrDefaultAsync();

            if (mfa == null)
            {
                mfa = new UserMfaToken { UserId = token.UserId, Token = token.Token };
                _context.UserMfaTokens.Add(mfa);
            }
            else
            {
                mfa.Token = token.Token;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string> GetMfaTokenAsync(int userId)
        {
            return await _context.UserMfaTokens
                .Where(x => x.UserId == userId)
                .Select(x => x.Token)
                .FirstOrDefaultAsync();
        }

        public async Task<UserMfaToken?> GetValidTokenAsync(int userId, string token)
        {
            return await _context.UserMfaTokens
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Token == token &&
                    !t.Used &&
                    t.ExpiresAt > DateTime.UtcNow);
        }
    }

}
