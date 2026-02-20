using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace EVWebApi.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public ICabinetRepository Cabinets { get; }
        public IUserRepository Users { get; }
        public IGroupRepository Groups { get; }
 

        public IDocumentRepository Documents { get; }
        public UnitOfWork(AppDbContext context,
        IUserRepository users,
        IGroupRepository groups,
        ICabinetRepository cabinets,
  
        IDocumentRepository documents)
        {
            _context = context;
            Users = users;
            Groups = groups;
            Cabinets = cabinets;
           
            Documents = documents;
        }


        //begin transaction
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
        //commit
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        //rollback
        public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
        }
    }
}
