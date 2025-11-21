using EVWebApi.DTOs;
using EVWebApi.Models;
using EVWebApi.Repositories;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role> GetByNameAsync(string roleName);

        IQueryable<Role> Query();


    }
}
