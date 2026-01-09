using EVWebApi.Models;

namespace EVWebApi.Interfaces.Services
{
    public interface IDocumentGroupingService
    {
        Task<List<string>> GetDynamicGroupingKeyAsync(int cabinetId);
    }
}
