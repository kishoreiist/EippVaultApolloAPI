

namespace EVWebApi.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IGroupRepository Groups { get; }
        ICabinetRepository Cabinets { get; }
        Task<int> CompleteAsync();
    }
}
