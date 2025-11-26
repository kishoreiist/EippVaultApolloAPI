namespace EVWebApi.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IGroupRepository Groups { get; }
        Task<int> CompleteAsync();
    }
}
