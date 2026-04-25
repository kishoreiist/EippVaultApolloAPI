using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController:BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotificationsByUser(int userId)
        {
            var result = await _notificationService.GetByUserIdAsync(userId);
            return Ok(new { data = result });
        }

        [HttpPost("mark_all_read/{userId}")]
        public async Task<IActionResult> MarkAllRead(int userId)
        {
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }

        [HttpPost("mark_read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }
    }
}
