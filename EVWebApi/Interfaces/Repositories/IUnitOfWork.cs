

using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IGroupRepository Groups { get; }
        ICabinetRepository Cabinets { get; }
        IDocumentRepository Documents { get; }
        Task<int> CompleteAsync();
    }
}
