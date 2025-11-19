using EVWebApi.Data;
using EVWebApi.Interfaces;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserMfaToken?> GetMfaTokenAsync(int userId)
        {
            return await _context.UserMfaTokens
                .OrderByDescending(t => t.TokenId)
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task AddMfaTokenAsync(UserMfaToken token)
        {
            _context.UserMfaTokens.Add(token);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}
