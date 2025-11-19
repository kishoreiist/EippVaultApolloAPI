namespace EVWebApi.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IGroupRepository Groups { get; }
        Task<int> CompleteAsync();
    }
}
