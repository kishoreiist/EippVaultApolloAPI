using EVWebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.BackgroundServices
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionCleanupService> _logger;

        public SessionCleanupService(IServiceScopeFactory scopeFactory,
                                     ILogger<SessionCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // run once per day (change if needed)
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupOldSessions();

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CleanupOldSessions()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoffDate = DateTime.UtcNow.AddDays(-60);

                var oldSessions = await db.UserSessions
                    .Where(s => s.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldSessions.Any())
                {
                    db.UserSessions.RemoveRange(oldSessions);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session cleanup failed");
            }
        }
    
    }
}
