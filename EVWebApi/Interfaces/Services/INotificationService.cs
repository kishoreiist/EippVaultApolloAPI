using EVWebApi.Models;

namespace EVWebApi.Interfaces.Services
{
    public interface INotificationService
    {
        Task CreateAsync(int userId, string message);
        Task<List<Notification>> GetByUserIdAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
    }
}
