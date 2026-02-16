using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeavesConfigController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public LeavesConfigController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        // GET: LeavesConfig
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var configs = campusId == null || campusId == 0
                ? await _context.LeaveConfigs
                    .Where(c => c.IsActive)
                    .Include(c => c.Campus)
                    .OrderBy(c => c.EmployeeType)
                    .ThenBy(c => c.LeaveType)
                    .ToListAsync()
                : await _context.LeaveConfigs
                    .Where(c => c.IsActive && c.CampusId == campusId)
                    .Include(c => c.Campus)
                    .OrderBy(c => c.EmployeeType)
                    .ThenBy(c => c.LeaveType)
                    .ToListAsync();

            return View(configs);
        }

        // GET: LeavesConfig/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CampusId"] = campusId == null || campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name")
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name");

            // Get employee types that have at least one unconfigured role
            var allEmployeeTypes = new[] { "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor" };
            var availableEmployeeTypes = new List<string>();

            foreach (var empType in allEmployeeTypes)
            {
                var hasUnconfiguredRoles = await HasUnconfiguredRoles(empType, campusId);
                if (hasUnconfiguredRoles)
                {
                    availableEmployeeTypes.Add(empType);
                }
            }

            ViewData["EmployeeTypes"] = new SelectList(availableEmployeeTypes);

            ViewData["LeaveTypes"] = new SelectList(new[]
            {
                "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave"
            });

            ViewData["AllocationPeriods"] = new SelectList(new[] { "Monthly", "Yearly" });

            return View();
        }

        // Helper method to check if an employee type has unconfigured roles
        private async Task<bool> HasUnconfiguredRoles(string employeeType, int? campusId)
        {
            // Get all roles for this employee type
            var rolesQuery = _context.EmployeeRoleConfigs
                .Where(rc => rc.IsActive && rc.EmployeeType == employeeType);

            if (campusId != null && campusId != 0)
            {
                rolesQuery = rolesQuery.Where(rc => rc.CampusId == campusId);
            }

            var roles = await rolesQuery.ToListAsync();
            
            if (!roles.Any())
                return false;

            // Check if any role is not fully configured (all leave types)
            foreach (var role in roles)
            {
                var configuredLeaveTypesCount = await _context.LeaveConfigs
                    .Where(lc => lc.IsActive && 
                                lc.EmployeeType == employeeType && 
                                lc.RoleName == role.RoleName)
                    .Select(lc => lc.LeaveType)
                    .Distinct()
                    .CountAsync();
                
                // If this role doesn't have all 6 leave types configured, it's available
                if (configuredLeaveTypesCount < 6)
                {
                    return true;
                }
            }

            return false;
        }

        // API to get available roles for selected employee type
        [HttpGet]
        public async Task<IActionResult> GetAvailableRoles(string employeeType)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var rolesQuery = _context.EmployeeRoleConfigs
                .Where(rc => rc.IsActive && rc.EmployeeType == employeeType);

            if (campusId != null && campusId != 0)
            {
                rolesQuery = rolesQuery.Where(rc => rc.CampusId == campusId);
            }

            var allRoles = await rolesQuery.ToListAsync();
            var availableRoles = new List<object>();

            foreach (var role in allRoles)
            {
                // Get configured leave types for this role
                var configuredLeaveTypes = await _context.LeaveConfigs
                    .Where(lc => lc.IsActive && 
                                lc.EmployeeType == employeeType && 
                                lc.RoleName == role.RoleName)
                    .Select(lc => lc.LeaveType)
                    .Distinct()
                    .ToListAsync();

                // Only include if not all leave types are configured
                if (configuredLeaveTypes.Count < 6)
                {
                    availableRoles.Add(new { 
                        id = role.Id, 
                        roleName = role.RoleName,
                        configuredLeaveTypes = configuredLeaveTypes
                    });
                }
            }

            return Json(availableRoles);
        }

        // POST: LeavesConfig/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeType,RoleName,LeaveType,AllocationPeriod,AllowedDays,IsCarryForward,MaxCarryForwardDays,CampusId")] LeaveConfig leaveConfig)
        {
            ModelState.Remove("Campus");
            ModelState.Remove("RoleNames");
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                leaveConfig.CreatedBy = currentUser?.UserName;
                leaveConfig.CreatedAt = DateTime.Now;
                
                _context.Add(leaveConfig);
                await _context.SaveChangesAsync();

                // Create leave balances for all employees with this role
                await CreateLeaveBalancesForRole(leaveConfig, currentUser?.UserName);
                
                TempData["SuccessMessage"] = "Leave configuration created successfully!";
                return RedirectToAction(nameof(Index));
            }

            var campusId = leaveConfig.CampusId;
            ViewData["CampusId"] = campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name")
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name");

            // Get employee types that have at least one unconfigured role
            var allEmployeeTypes = new[] { "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor" };
            var availableEmployeeTypes = new List<string>();

            foreach (var empType in allEmployeeTypes)
            {
                var hasUnconfiguredRoles = await HasUnconfiguredRoles(empType, campusId == 0 ? null : (int?)campusId);
                if (hasUnconfiguredRoles)
                {
                    availableEmployeeTypes.Add(empType);
                }
            }

            ViewData["EmployeeTypes"] = new SelectList(availableEmployeeTypes);

            ViewData["LeaveTypes"] = new SelectList(new[]
            {
                "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave"
            });

            ViewData["AllocationPeriods"] = new SelectList(new[] { "Monthly", "Yearly" });

            return View(leaveConfig);
        }

        // GET: LeavesConfig/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveConfig = await _context.LeaveConfigs.FindAsync(id);
            if (leaveConfig == null || !leaveConfig.IsActive)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CampusId"] = campusId == null || campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", leaveConfig.CampusId)
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name", leaveConfig.CampusId);

            ViewData["EmployeeTypes"] = new SelectList(new[]
            {
                "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor"
            }, leaveConfig.EmployeeType);

            ViewData["LeaveTypes"] = new SelectList(new[]
            {
                "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave"
            }, leaveConfig.LeaveType);

            ViewData["AllocationPeriods"] = new SelectList(new[] { "Monthly", "Yearly" }, leaveConfig.AllocationPeriod);

            return View(leaveConfig);
        }

        // POST: LeavesConfig/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmployeeType,RoleName,LeaveType,AllocationPeriod,AllowedDays,IsCarryForward,MaxCarryForwardDays,CampusId,IsActive,CreatedAt,CreatedBy")] LeaveConfig leaveConfig)
        {
            if (id != leaveConfig.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    leaveConfig.UpdatedBy = currentUser?.UserName;
                    leaveConfig.UpdatedAt = DateTime.Now;
                    
                    _context.Update(leaveConfig);
                    await _context.SaveChangesAsync();

                    // Update leave balances for employees with this role
                    await UpdateLeaveBalancesForConfig(leaveConfig, currentUser?.UserName);
                    
                    TempData["SuccessMessage"] = "Leave configuration updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveConfigExists(leaveConfig.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var campusId = leaveConfig.CampusId;
            ViewData["CampusId"] = campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", leaveConfig.CampusId)
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name", leaveConfig.CampusId);

            ViewData["EmployeeTypes"] = new SelectList(new[]
            {
                "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor"
            }, leaveConfig.EmployeeType);

            ViewData["LeaveTypes"] = new SelectList(new[]
            {
                "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave"
            }, leaveConfig.LeaveType);

            ViewData["AllocationPeriods"] = new SelectList(new[] { "Monthly", "Yearly" }, leaveConfig.AllocationPeriod);

            return View(leaveConfig);
        }

        // POST: LeavesConfig/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var leaveConfig = await _context.LeaveConfigs.FindAsync(id);
            if (leaveConfig != null)
            {
                leaveConfig.IsActive = false;
                var currentUser = await _userManager.GetUserAsync(User);
                leaveConfig.UpdatedBy = currentUser?.UserName;
                leaveConfig.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Leave configuration deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LeaveConfigExists(int id)
        {
            return _context.LeaveConfigs.Any(e => e.Id == id);
        }

        // Helper method to create leave balances for employees with a specific role and leave config
        private async Task CreateLeaveBalancesForRole(LeaveConfig leaveConfig, string? createdBy)
        {
            // Use a consistent timestamp for all operations
            var currentDateTime = DateTime.Now;
            var currentYear = currentDateTime.Year;
            var currentMonth = currentDateTime.Month;

            // Find all active employees who have the specified role assigned
            var employeesWithRole = await _context.EmployeeRoles
                .Where(er => er.IsActive && 
                            er.EmployeeRoleConfig.RoleName == leaveConfig.RoleName &&
                            er.EmployeeRoleConfig.EmployeeType == leaveConfig.EmployeeType &&
                            er.CampusId == leaveConfig.CampusId)
                .Include(er => er.Employee)
                .Select(er => er.Employee)
                .Distinct()
                .ToListAsync();

            if (!employeesWithRole.Any())
                return;

            // Load all existing balances for these employees in a single query to avoid N+1
            var employeeIds = employeesWithRole.Select(e => e.Id).ToList();
            var existingBalances = await _context.LeaveBalances
                .Where(lb => 
                    employeeIds.Contains(lb.EmployeeId) &&
                    lb.LeaveType == leaveConfig.LeaveType &&
                    lb.Year == currentYear &&
                    lb.CampusId == leaveConfig.CampusId)
                .ToListAsync();

            foreach (var employee in employeesWithRole)
            {
                // Check if leave balance already exists for this employee, leave type, and period
                var existingBalance = existingBalances.FirstOrDefault(lb => 
                    lb.EmployeeId == employee.Id &&
                    (leaveConfig.AllocationPeriod == "Yearly" ? lb.Month == null : lb.Month == currentMonth));

                if (existingBalance == null)
                {
                    var leaveBalance = new LeaveBalance
                    {
                        EmployeeId = employee.Id,
                        LeaveType = leaveConfig.LeaveType,
                        Year = currentYear,
                        Month = leaveConfig.AllocationPeriod == "Monthly" ? currentMonth : (int?)null,
                        TotalAllocated = leaveConfig.AllowedDays,
                        Used = 0,
                        CarriedForward = 0,
                        CreatedBy = createdBy ?? "System",
                        CreatedAt = currentDateTime,
                        CampusId = leaveConfig.CampusId
                    };

                    _context.LeaveBalances.Add(leaveBalance);
                }
            }

            await _context.SaveChangesAsync();
        }

        // Helper method to update leave balances when config is edited
        private async Task UpdateLeaveBalancesForConfig(LeaveConfig leaveConfig, string? updatedBy)
        {
            // Use a consistent timestamp for all operations
            var currentDateTime = DateTime.Now;
            var currentYear = currentDateTime.Year;
            var currentMonth = currentDateTime.Month;

            // Find all leave balances that match this config
            var leaveBalances = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .Where(lb => 
                    lb.LeaveType == leaveConfig.LeaveType &&
                    lb.Year == currentYear &&
                    lb.CampusId == leaveConfig.CampusId)
                .ToListAsync();

            if (!leaveBalances.Any())
                return;

            // Load all employee roles for these employees in a single query to avoid N+1
            var employeeIds = leaveBalances.Select(lb => lb.EmployeeId).Distinct().ToList();
            var employeeRoles = await _context.EmployeeRoles
                .Where(er => 
                    employeeIds.Contains(er.EmployeeId) &&
                    er.IsActive &&
                    er.EmployeeRoleConfig.RoleName == leaveConfig.RoleName &&
                    er.EmployeeRoleConfig.EmployeeType == leaveConfig.EmployeeType &&
                    er.CampusId == leaveConfig.CampusId)
                .Select(er => er.EmployeeId)
                .Distinct()
                .ToListAsync();

            foreach (var balance in leaveBalances)
            {
                // Verify this employee has the role from the config
                if (employeeRoles.Contains(balance.EmployeeId))
                {
                    // Check if this balance matches the allocation period
                    bool matchesPeriod = (leaveConfig.AllocationPeriod == "Yearly" && balance.Month == null) ||
                                        (leaveConfig.AllocationPeriod == "Monthly" && balance.Month == currentMonth);

                    if (matchesPeriod)
                    {
                        balance.TotalAllocated = leaveConfig.AllowedDays;
                        balance.UpdatedBy = updatedBy ?? "System";
                        balance.UpdatedAt = currentDateTime;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
