using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Services
{
    public class DocumentGroupingService : IDocumentGroupingService
    {
        private new readonly AppDbContext _context;
        private readonly IDocumentRepository _docrepo;
        public DocumentGroupingService(AppDbContext context, IDocumentRepository docrepo) 
        {
            _context = context;
            _docrepo = docrepo;
        }

        public async Task<List<string>> GetDynamicGroupingKeyAsync(int cabinetId,string action)
        {
            var columns = new List<string>();
            if (action=="upload")
            {   
                columns = await _docrepo.GetCabinetUploadColumns(cabinetId);
                
            }
            else if(action=="grouping")
            {
                 columns = await _docrepo.GetCabinetGroupingColumns(cabinetId);
            }
             else
            {
                throw new ArgumentException($"Invalid action: {action}. Expected 'upload' or 'grouping'.");
            }
            //columns = await _docrepo.GetCabinetGroupingColumns(cabinetId);
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
