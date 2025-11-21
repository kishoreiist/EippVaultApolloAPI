using EVWebApi.Models;
using EVWebApi.Repositories;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        //Task<IEnumerable<User>> GetAllAsync();

        IQueryable<User> Query();   
    }
}
