using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Services
{
    public class LeaveBalanceRolloverService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaveBalanceRolloverService> _logger;
        private Timer? _timer;

        public LeaveBalanceRolloverService(
            IServiceProvider serviceProvider,
            ILogger<LeaveBalanceRolloverService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Leave Balance Rollover Service is starting.");

            // Run daily at midnight
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(1); // Tomorrow at midnight
            var firstDelay = nextRun - now;

            _timer = new Timer(
                DoWork,
                null,
                firstDelay,
                TimeSpan.FromDays(1)); // Repeat daily

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation("Leave Balance Rollover Service is working.");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                await ProcessMonthlyRollover(context);
                await ProcessYearlyRollover(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during leave balance rollover.");
            }
        }

        private async Task ProcessMonthlyRollover(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            
            // Check if it's the first day of the month
            if (today.Day != 1)
                return;

            _logger.LogInformation("Processing monthly leave balance rollover...");

            var currentYear = today.Year;
            var currentMonth = today.Month;
            var previousMonth = today.AddMonths(-1).Month;
            var previousYear = today.AddMonths(-1).Year;

            // Get all monthly leave configurations
            var monthlyLeaveConfigs = await context.LeaveConfigs
                .Where(lc => lc.IsActive && lc.AllocationPeriod == "Monthly")
                .ToListAsync();

            // Get all active employees with roles
            var activeEmployees = await context.EmployeeRoles
                .Where(er => er.IsActive && !er.ToDate.HasValue)
                .Include(er => er.Employee)
                .Include(er => er.EmployeeRoleConfig)
                .ToListAsync();

            foreach (var employeeRole in activeEmployees)
            {
                // Get leave configs for this employee's role
                var employeeLeaveConfigs = monthlyLeaveConfigs
                    .Where(lc => lc.EmployeeType == employeeRole.EmployeeRoleConfig.EmployeeType &&
                                lc.RoleName == employeeRole.EmployeeRoleConfig.RoleName)
                    .ToList();

                foreach (var leaveConfig in employeeLeaveConfigs)
                {
                    // Check if balance already exists for current month
                    var existingBalance = await context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeRole.EmployeeId &&
                                                  lb.LeaveType == leaveConfig.LeaveType &&
                                                  lb.Year == currentYear &&
                                                  lb.Month == currentMonth);

                    if (existingBalance != null)
                        continue; // Already processed

                    // Get previous month's balance
                    var previousBalance = await context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeRole.EmployeeId &&
                                                  lb.LeaveType == leaveConfig.LeaveType &&
                                                  lb.Year == previousYear &&
                                                  lb.Month == previousMonth);

                    decimal carriedForward = 0;
                    
                    // Calculate carry forward if enabled
                    if (leaveConfig.IsCarryForward && previousBalance != null)
                    {
                        var availableFromPrevious = previousBalance.Available;
                        if (availableFromPrevious > 0)
                        {
                            if (leaveConfig.MaxCarryForwardDays.HasValue && leaveConfig.MaxCarryForwardDays.Value > 0)
                            {
                                carriedForward = Math.Min(availableFromPrevious, leaveConfig.MaxCarryForwardDays.Value);
                            }
                            else
                            {
                                carriedForward = availableFromPrevious;
                            }
                        }
                    }

                    // Create new balance for current month
                    var newBalance = new LeaveBalance
                    {
                        EmployeeId = employeeRole.EmployeeId,
                        LeaveType = leaveConfig.LeaveType,
                        Year = currentYear,
                        Month = currentMonth,
                        TotalAllocated = leaveConfig.AllowedDays,
                        Used = 0,
                        CarriedForward = carriedForward,
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now,
                        CampusId = employeeRole.CampusId
                    };

                    context.LeaveBalances.Add(newBalance);

                    // Record history
                    context.LeaveBalanceHistories.Add(new LeaveBalanceHistory
                    {
                        EmployeeId = employeeRole.EmployeeId,
                        LeaveType = leaveConfig.LeaveType,
                        ActionType = "MonthlyRollover",
                        Amount = leaveConfig.AllowedDays,
                        BalanceBefore = 0,
                        BalanceAfter = newBalance.TotalAllocated + carriedForward,
                        Remarks = $"Monthly allocation for {today:MMMM yyyy}" + (carriedForward > 0 ? $" with {carriedForward} days carried forward" : ""),
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now,
                        CampusId = employeeRole.CampusId
                    });

                    _logger.LogInformation($"Created monthly leave balance for Employee {employeeRole.EmployeeId}, {leaveConfig.LeaveType}: {leaveConfig.AllowedDays} days" + (carriedForward > 0 ? $" + {carriedForward} carried forward" : ""));
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Monthly leave balance rollover completed.");
        }

        private async Task ProcessYearlyRollover(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            
            // Check if it's January 1st
            if (today.Month != 1 || today.Day != 1)
                return;

            _logger.LogInformation("Processing yearly leave balance rollover...");

            var currentYear = today.Year;
            var previousYear = currentYear - 1;

            // Get all yearly leave configurations
            var yearlyLeaveConfigs = await context.LeaveConfigs
                .Where(lc => lc.IsActive && lc.AllocationPeriod == "Yearly")
                .ToListAsync();

            // Get all active employees with roles
            var activeEmployees = await context.EmployeeRoles
                .Where(er => er.IsActive && !er.ToDate.HasValue)
                .Include(er => er.Employee)
                .Include(er => er.EmployeeRoleConfig)
                .ToListAsync();

            foreach (var employeeRole in activeEmployees)
            {
                // Get leave configs for this employee's role
                var employeeLeaveConfigs = yearlyLeaveConfigs
                    .Where(lc => lc.EmployeeType == employeeRole.EmployeeRoleConfig.EmployeeType &&
                                lc.RoleName == employeeRole.EmployeeRoleConfig.RoleName)
                    .ToList();

                foreach (var leaveConfig in employeeLeaveConfigs)
                {
                    // Check if balance already exists for current year
                    var existingBalance = await context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeRole.EmployeeId &&
                                                  lb.LeaveType == leaveConfig.LeaveType &&
                                                  lb.Year == currentYear &&
                                                  lb.Month == null);

                    if (existingBalance != null)
                        continue; // Already processed

                    // Get previous year's balance
                    var previousBalance = await context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeRole.EmployeeId &&
                                                  lb.LeaveType == leaveConfig.LeaveType &&
                                                  lb.Year == previousYear &&
                                                  lb.Month == null);

                    decimal carriedForward = 0;
                    
                    // Calculate carry forward if enabled
                    if (leaveConfig.IsCarryForward && previousBalance != null)
                    {
                        var availableFromPrevious = previousBalance.Available;
                        if (availableFromPrevious > 0)
                        {
                            if (leaveConfig.MaxCarryForwardDays.HasValue && leaveConfig.MaxCarryForwardDays.Value > 0)
                            {
                                carriedForward = Math.Min(availableFromPrevious, leaveConfig.MaxCarryForwardDays.Value);
                            }
                            else
                            {
                                carriedForward = availableFromPrevious;
                            }
                        }
                    }

                    // Create new balance for current year
                    var newBalance = new LeaveBalance
                    {
                        EmployeeId = employeeRole.EmployeeId,
                        LeaveType = leaveConfig.LeaveType,
                        Year = currentYear,
                        Month = null,
                        TotalAllocated = leaveConfig.AllowedDays,
                        Used = 0,
                        CarriedForward = carriedForward,
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now,
                        CampusId = employeeRole.CampusId
                    };

                    context.LeaveBalances.Add(newBalance);

                    // Record history
                    context.LeaveBalanceHistories.Add(new LeaveBalanceHistory
                    {
                        EmployeeId = employeeRole.EmployeeId,
                        LeaveType = leaveConfig.LeaveType,
                        ActionType = "YearlyRollover",
                        Amount = leaveConfig.AllowedDays,
                        BalanceBefore = 0,
                        BalanceAfter = newBalance.TotalAllocated + carriedForward,
                        Remarks = $"Yearly allocation for {currentYear}" + (carriedForward > 0 ? $" with {carriedForward} days carried forward" : ""),
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now,
                        CampusId = employeeRole.CampusId
                    });

                    _logger.LogInformation($"Created yearly leave balance for Employee {employeeRole.EmployeeId}, {leaveConfig.LeaveType}: {leaveConfig.AllowedDays} days" + (carriedForward > 0 ? $" + {carriedForward} carried forward" : ""));
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Yearly leave balance rollover completed.");
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
