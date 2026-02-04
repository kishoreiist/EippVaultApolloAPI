using EVWebApi.Data;
using EVWebApi.DTOs.Group;
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
        public async Task<List<GroupListDto>> GetGroupsForDropdownAsync()
        {
            return await _context.Groups
                .AsNoTracking()
                .Select(g => new GroupListDto
                {
                    Id = g.GroupId,
                    Name = g.GroupName,
                    UserType = g.UserType
                })
                .ToListAsync();
        }

        public async Task<bool> GetUsersAsync(int id)
        {
            return await _context.Users.AnyAsync(u => u.UserGroup.GroupId == id);
        }

        //--------------email  group----------------
        public async Task<List<EmailGroupUserDto>> GetUsersByEmailGroupIdAsync(int emailGroupId)
        {
            return await _context.Users
                .Where(u => u.EmailGroupId == emailGroupId)
                .Select(u => new EmailGroupUserDto
                {
                    UserId = u.UserId,
                    Email = u.Email
                })
                .ToListAsync();
        }


        public async Task<EmailGroup> GetByEmailGroupnameAsync(string group_name)
        {
            return await _context.EmailGroups
                .FirstOrDefaultAsync(g => g.GroupName.ToLower() == group_name.ToLower());
        }

        public async Task<EmailGroup> AddEmailGroupAsync(EmailGroup group)
        {
            _context.EmailGroups.Add(group);
            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<List<EmailGroup>> GeAllEmailGroupsAsync()
        {
            return await _context.EmailGroups
                .AsNoTracking()
                //.Select(g => new ListDto
                //{
                //    Id = g.Id,
                //    Name = g.GroupName
                //})
                .ToListAsync();
        }

        public async Task<EmailGroup> GetEmailGroupByIdAsync(int id)
        {
            return await _context.EmailGroups
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async void UpdateEmailGroupAsync(EmailGroup group)
        {
            _context.EmailGroups.Update(group);
            await _context.SaveChangesAsync();
        }
        public void RemoveEmailGroupAsync(EmailGroup group) {
            _context.EmailGroups.Remove(group);
        }
    }
}
