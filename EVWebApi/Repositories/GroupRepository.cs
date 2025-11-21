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
                .FirstOrDefaultAsync(u => u.GroupName == groupname);
        }
        //public async Task<IEnumerable<Group>> GetAllAsync()
        //{
        //    return await _context.Groups.ToListAsync();
        //}

        public IQueryable<Group> Query()
        {
            return _context.Groups.AsQueryable();
        }

    }
}
