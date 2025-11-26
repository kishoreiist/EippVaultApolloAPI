using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private new readonly AppDbContext _context;
        public UserRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }


        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public override async Task<User?> GetByIdAsync(int id)
        { 
            return await Query()
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public IQueryable<User> Query()
        {
            return _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserGroups)
                    .ThenInclude(ug => ug.Group)
                .AsQueryable();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

    }
}
