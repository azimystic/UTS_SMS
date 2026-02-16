using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UTS_SMS.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

        public NotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotifications();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking and sending notifications.");
                }

                // Wait for the next interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Background Service is stopping.");
        }

        private async Task CheckAndSendNotifications()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                _logger.LogInformation("Checking for pending marks entry reminders...");
                await notificationService.CheckAndSendMarksEntryReminders();

                _logger.LogInformation("Checking for pending diary entry reminders...");
                await notificationService.CheckAndSendDiaryReminders();

                _logger.LogInformation("Notification check completed.");
            }
        }
    }
}
