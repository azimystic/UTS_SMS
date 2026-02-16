using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS
{
    public class SalaryDeductionBackgroundService : BackgroundService
    {
        private readonly ILogger<SalaryDeductionBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SalaryDeductionBackgroundService(
            ILogger<SalaryDeductionBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Salary Deduction Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var targetDate = new DateTime(now.Year, now.Month, 1);

                    // If we're past the 5th, target next month's 5th
                    //if (now.Day > 5)
                    //{
                    //    targetDate = targetDate.AddMonths(1);
                    //}

                    var delay = targetDate - now;

                    if (delay.TotalHours < 0)
                    {
                        _logger.LogInformation($"Checking if salary deductions need to be processed...");
                        
                        // Process if it's the 5th or later (in case service was down on 5th)
                        if (now.Day >= 0)
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                var Month = now.AddMonths(0);
                                
                                // Check if we've already processed for this month
                                var lastProcessed = await context.SalaryDeductions
                                    .Where(sd => sd.ForMonth == Month.Month && 
                                                 sd.ForYear == Month.Year)
                                    .AnyAsync(stoppingToken);

                                if (!lastProcessed)
                                {
                                    var deductionService = scope.ServiceProvider.GetRequiredService<ISalaryDeductionService>();
                                    await deductionService.ProcessMonthlyDeductionsAsync();
                                    _logger.LogInformation("Salary deductions processed successfully");
                                }
                            }
                        }

                        // Check again in 1 hour
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    else
                    {
                        // Wait until closer to the target date
                        await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Salary Deduction Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }

            _logger.LogInformation("Salary Deduction Background Service is stopping.");
        }
    }
}
