using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;

namespace EVWebApi.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;


        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public IGroupRepository Groups { get; }
        public UnitOfWork(AppDbContext context,
        IUserRepository users,
        IRoleRepository roles,
        IGroupRepository groups)
        {
            _context = context;
            Users = users;
            Roles = roles;
            Groups = groups;
        }


        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
