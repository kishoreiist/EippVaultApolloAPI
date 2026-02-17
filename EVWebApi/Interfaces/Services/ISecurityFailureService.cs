namespace EVWebApi.Interfaces.Services
{
    public interface ISecurityFailureService
    {
        Task RegisterFailureAsync(int? userId, string? ip, string endpoint);
        Task<bool> IsIpBlacklistedAsync(string ip);
        Task<bool> IsUserLockedAsync(int userId);
    }
}
