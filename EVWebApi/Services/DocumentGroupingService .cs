using EVWebApi.Data;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Services
{
    public class DocumentGroupingService : IDocumentGroupingService
    {
        private new readonly AppDbContext _context;
        public DocumentGroupingService(AppDbContext context) 
        {
            _context = context;
        }

        public async Task<List<string>> GetDynamicGroupingKeyAsync(int cabinetId)
        {

            var columns = await _context.CabinetGroupingRules
                .Where(c => c.Cabinet.CabinetId == cabinetId)
                .OrderBy(c => c.GroupingOrder)
                .Select(c => c.GroupingCol)
                .ToListAsync();

            if (!columns.Any())
            {
                throw new KeyNotFoundException($"Configuration Error: No grouping rules defined for Cabinet ID {cabinetId}.");
            }


            // Maping DB Columns to EF Property Names
            var entityType = _context.Model.FindEntityType(typeof(Document));

            var props = columns.Select(col =>
                entityType.GetProperties()
                    .FirstOrDefault(
                    p => string.Equals(p.GetColumnName(), col, StringComparison.OrdinalIgnoreCase))?.Name
                ).Where(p => !string.IsNullOrEmpty(p)).ToList();

            if (!props.Any())
            {
                throw new InvalidOperationException($"Configuration Error: The grouping rule columns provided for Cabinet {cabinetId} do not match any valid fields.");
            }

            return props;
        }
    }
       
}
