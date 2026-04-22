

using EVWebApi.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IGroupRepository Groups { get; }
        ICabinetRepository Cabinets { get; }
        IDocumentRepository Documents { get; }
        IDocVersionRepository DocumentVersions { get; }
        Task<int> CompleteAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task RollbackTransactionAsync(IDbContextTransaction transaction);
    }
}
