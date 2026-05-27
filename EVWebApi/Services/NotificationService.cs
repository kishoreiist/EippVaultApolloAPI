using EVWebApi.Data;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Services
{
    public class NotificationService: INotificationService
    {

        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(int userId, string message)
        {
            try
            {

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Status = "unread",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<Notification>> GetByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notif = await _context.Notifications.FindAsync(notificationId);

            if (notif != null && notif.Status == "unread")
            {
                notif.Status = "read";
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifs = await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == "unread")
                .ToListAsync();

            foreach (var n in notifs)
            {
                n.Status = "read";
            }

            await _context.SaveChangesAsync();
        }
    }
}
