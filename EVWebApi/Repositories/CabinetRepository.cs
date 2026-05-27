using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Repositories
{
    public class CabinetRepository : GenericRepository<Cabinet>, ICabinetRepository
    {
        private new readonly AppDbContext _context;
        public CabinetRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Cabinet> GetByCabinetnameAsync(string cabinetname)
        {
            return await _context.Cabinets
                .FirstOrDefaultAsync(u => u.CabinetName == cabinetname);
        }
        public async Task<string> GetCabinetNameAsync(int cabinetId)
        {
             var cabinet =  await _context.Cabinets
                        .FirstOrDefaultAsync(u => u.CabinetId == cabinetId);
            return cabinet != null ? cabinet.CabinetName : "Cabinet not found!";

        }

        public override async Task<Cabinet?> GetByIdAsync(int id)
        {
            return await Query()
                .FirstOrDefaultAsync(u => u.CabinetId == id);
        }

        public IQueryable<Cabinet> Query()
        {
            return _context.Cabinets.AsQueryable();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
