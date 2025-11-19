
using EVWebApi.Models;

namespace EVWebApi.Interfaces
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<Group> GetByGroupnameAsync(string groupname);
        Task<IEnumerable<Group>> GetAllAsync();
    }
}
