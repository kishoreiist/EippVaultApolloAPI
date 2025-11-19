using EVWebApi.Data;
using EVWebApi.Interfaces;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        private new readonly AppDbContext _context;
        public RoleRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }


        public async Task<Role> GetByNameAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        }
        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync();
        }


    }
}
