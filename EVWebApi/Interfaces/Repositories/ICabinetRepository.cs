using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface ICabinetRepository : IGenericRepository<Cabinet>
    {
        Task<Cabinet> GetByCabinetnameAsync(string cabinetname);
        Task<string> GetCabinetNameAsync(int cabinetId);
        Task<IEnumerable<Cabinet>> GetAllAsync();
        Task<int> SaveChangesAsync();
        IQueryable<Cabinet> Query();
    }
}
