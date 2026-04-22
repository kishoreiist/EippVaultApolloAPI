using EVWebApi.Data;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Settings;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
namespace EVWebApi.BackgroundServices
{
    public class StorageMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly StorageAlertEmailSettings _emailSettings;
        private readonly ILogger<StorageMonitorService> _logger;
        public StorageMonitorService(
            IServiceScopeFactory scopeFactory,
            ILogger<StorageMonitorService> logger,
            IOptions<StorageAlertEmailSettings> emailSettings
            )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StorageMonitorService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("StorageMonitorService checking storage...");
                await CheckStorage();

               await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckStorage()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    var plan = await dbContext.ClientPlanDetail.ToListAsync();
                    foreach (var p in plan)
                    {
                       
                        var basePath = p.StorageRoot;
                        var clientName = p.ClientName;
                        long sizeBytes = p.UsedSizeBytes;
                        long planSizeBytes = p.PlanSizeBytes;

                        var cooldown = TimeSpan.FromHours(24);

                        bool canSendAgain = p.LastAlertSentAt == null ||
                                            p.LastAlertSentAt < DateTime.UtcNow - cooldown;


                        decimal usedGb = sizeBytes / (1024m * 1024m * 1024m);
                        decimal totalGb = planSizeBytes / (1024m * 1024m * 1024m);


                        decimal firstAlertBytes = planSizeBytes * 0.8m;
                        decimal finalAlertBytes = planSizeBytes * 0.9m;

                        if (!Directory.Exists(basePath))
                        {
                            //_logger.LogWarning($"Folder missing for {_clientName}");
                            _logger.LogWarning("Folder missing for {ClientName}", clientName);
                            continue;
                        }

                        if (sizeBytes >= finalAlertBytes && (!p.FinalAlertSent || canSendAgain))
                        {
                            await emailService.SendAsync(
                                _emailSettings.To,
                                _emailSettings.ReplyTo,
                                _emailSettings.UserName,
                                "Storage Usage Alert – 90% Limit Reached",
                                $"<p><b>{clientName}</b>  has reached 90 % of its allocated storage.</p>" +
                                $"<p>Current usage: {usedGb:F2} GB of {totalGb:F2} GB.Please clean up or upgrade.</p>" +
                                $"<p>Please clean up unused files or upgrade the storage to avoid upload interruptions.</p>",
                                _emailSettings.Cc
                             );

                            _logger.LogWarning(
                                $"Client {clientName} storage warning {usedGb:F2}GB of {totalGb:F2}GB");
                            p.FinalAlertSent = true;
                            p.LastAlertSentAt = DateTime.UtcNow;
                        }
                        else if (sizeBytes >= firstAlertBytes && (!p.FirstAlertSent || canSendAgain))
                        {
                            await emailService.SendAsync(
                                _emailSettings.To,
                                _emailSettings.ReplyTo,
                                _emailSettings.UserName,
                                "Storage Usage Alert – 80% Limit Reached",
                                $"<p><b>{clientName}</b>  has reached 80 % of its allocated storage.</p>" +
                                $"<p>Current usage: {usedGb:F2} GB of {totalGb:F2} GB.Please clean up or upgrade.</p>" +
                                $"<p>Please clean up unused files or upgrade the storage to avoid upload interruptions.</p>",
                                _emailSettings.Cc
                            );


                            _logger.LogWarning(
                                $"Client {clientName} storage warning {usedGb:F2} GB of {totalGb:F2} GB");
                            p.FirstAlertSent = true;
                            p.LastAlertSentAt = DateTime.UtcNow;
                        }

                        if (sizeBytes >= planSizeBytes)
                        {
                            throw new LockedException("Storage limit exceeded. Please upgrade plan.");
                        }

                        if (sizeBytes < firstAlertBytes && (p.FirstAlertSent || p.FinalAlertSent))
                        {
                            p.FirstAlertSent = false;
                            p.FinalAlertSent = false;
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storage monitor failed");
            }
        }

    }
    
    
}
