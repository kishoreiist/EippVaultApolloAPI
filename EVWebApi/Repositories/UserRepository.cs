using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using EVWebApi.Models.Security;
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
                .Include(u => u.UserGroup)
                    .ThenInclude(ug => ug.Group)
                        .ThenInclude(g => g.GroupAccessRights)
                        .ThenInclude(a=>a.AccessRight)

               .Include(u => u.UserGroup)
                    .ThenInclude(ug => ug.Group)
                        .ThenInclude(g => g.GroupCabinets)
                            .ThenInclude(c=>c.Cabinet)
                .Where(u => u.Status != UserStatus.Deleted && u.Status != UserStatus.Locked)
                .OrderBy(u=>u.UserId)
                .AsQueryable();
        }

        public void SoftDelete(User user)
        {
            user.Status = UserStatus.Deleted;
            user.UpdatedAt = DateTime.UtcNow;
            
            _dbSet.Update(user);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<string> GetUserType(int userId)
        {
            var userType = await _context.UserGroups
            .Where(x => x.UserId == userId)
            .Select(x => x.Group.UserType)
            .FirstOrDefaultAsync() ?? string.Empty;
            return userType;
        }

        public async Task<User?> GetByIdIncludingLockedAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id
                                      && u.Status != UserStatus.Deleted);
        }

        //USER PASSWORD METHODS
        public async Task<List<UserPasswordHistory>> GetLast5PasswordsAsync(int userId)
        {
            return await _context.UserPasswords
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
        public async Task AddPasswordHistoryAsync(UserPasswordHistory entity)
        {
            await _context.UserPasswords.AddAsync(entity);
        }
        public async Task DeleteOlderPasswordsAsync(int userId, int keepLast)
        {
            var toDelete = await _context.UserPasswords
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip(keepLast)
                .ToListAsync();

            _context.UserPasswords.RemoveRange(toDelete);
        }
    }
}
