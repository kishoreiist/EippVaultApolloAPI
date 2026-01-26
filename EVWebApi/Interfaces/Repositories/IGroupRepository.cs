using EVWebApi.DTOs.Group;
using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<Group> GetByGroupnameAsync(string groupname);
        Task<List<ListDto>> GetGroupsForDropdownAsync();
        IQueryable<Group> Query();
        Task<bool> GetUsersAsync(int id);
    }
}
