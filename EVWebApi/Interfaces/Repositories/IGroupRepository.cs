using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<Group> GetByGroupnameAsync(string groupname);
        //Task<IEnumerable<Group>> GetAllAsync();

        IQueryable<Group> Query();
    }
}
