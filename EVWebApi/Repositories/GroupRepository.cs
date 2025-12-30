using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;


namespace EVWebApi.Repositories
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        private new readonly AppDbContext _context;
        public GroupRepository(AppDbContext context) : base(context) 
        {
            _context = context;
        }
        public async Task<Group> GetByGroupnameAsync(string groupname)
        {
            return await _context.Groups
                .FirstOrDefaultAsync(u => u.GroupName.ToLower() == groupname.ToLower());
        }

        public override async Task<Group?> GetByIdAsync(int id)
        {
            return await Query()
                .FirstOrDefaultAsync(g => g.GroupId == id);
        }

        public IQueryable<Group> Query()
        {
            return _context.Groups
                 .Include(g => g.GroupAccessRights)
                        .ThenInclude(a => a.AccessRight)
                .Include(g=>g.GroupCabinets)
                    .ThenInclude(c=>c.Cabinet)
                .AsQueryable();
        }

    }
}
