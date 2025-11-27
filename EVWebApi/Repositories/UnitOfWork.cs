using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;

namespace EVWebApi.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public ICabinetRepository Cabinets { get; }
        public IUserRepository Users { get; }
        public IGroupRepository Groups { get; }
        public UnitOfWork(AppDbContext context,
        IUserRepository users,
        IGroupRepository groups,
         ICabinetRepository cabinets)
        {
            _context = context;
            Users = users;
            Groups = groups;
            Cabinets = cabinets;
        }


        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
