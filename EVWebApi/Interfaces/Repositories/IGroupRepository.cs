using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<Group> GetByGroupnameAsync(string groupname);

        IQueryable<Group> Query();
    }
}
