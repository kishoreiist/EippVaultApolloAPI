using System.Threading.Tasks;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using EVWebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class UserAuthenticatorRepository : IUserAuthenticatorRepository
    {
        private readonly AppDbContext _context;

        public UserAuthenticatorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(UserAuthenticator authenticator)
        {
            var existing = await _context.UserAuthenticators
                .FirstOrDefaultAsync(a => a.UserId == authenticator.UserId);

            if (existing == null)
            {
                _context.UserAuthenticators.Add(authenticator);
            }
            else
            {
                existing.SecretKey = authenticator.SecretKey;
                existing.Enabled = authenticator.Enabled;
                // Add other property updates as needed
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserAuthenticator?> GetByUserIdAsync(int userId)
        {
            return await _context.UserAuthenticators
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }
    }
}