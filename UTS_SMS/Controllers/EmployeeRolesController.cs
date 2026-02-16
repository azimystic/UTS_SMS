using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeRolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public EmployeeRolesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        // GET: EmployeeRoles
        public async Task<IActionResult> Index(int? employeeId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var employeeRolesQuery = _context.EmployeeRoles
                .Include(er => er.Employee)
                .Include(er => er.EmployeeRoleConfig)
                .Include(er => er.Campus)
                .Where(er => er.IsActive && !er.ToDate.HasValue) // Only show active roles
                .AsQueryable();

            // Filter by campus
            if (campusId != null && campusId != 0)
            {
                employeeRolesQuery = employeeRolesQuery.Where(er => er.CampusId == campusId);
            }

            // Filter by employee if specified
            if (employeeId.HasValue)
            {
                employeeRolesQuery = employeeRolesQuery.Where(er => er.EmployeeId == employeeId);
                var employee = await _context.Employees.FindAsync(employeeId);
                ViewBag.EmployeeName = employee?.FullName;
            }

            var employeeRoles = await employeeRolesQuery
                .OrderByDescending(er => er.FromDate)
                .ToListAsync();

            // Get available role configs for modal
            var roleConfigs = campusId != null && campusId != 0
                ? await _context.EmployeeRoleConfigs.Where(rc => rc.IsActive && rc.CampusId == campusId).ToListAsync()
                : await _context.EmployeeRoleConfigs.Where(rc => rc.IsActive).ToListAsync();
            
            ViewBag.RoleConfigs = roleConfigs;

            return View(employeeRoles);
        }

        // GET: EmployeeRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeRole = await _context.EmployeeRoles
                .Include(er => er.Employee)
                .Include(er => er.EmployeeRoleConfig)
                .Include(er => er.Campus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employeeRole == null)
            {
                return NotFound();
            }

            return View(employeeRole);
        }

        // POST: EmployeeRoles/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int employeeRoleId, int newRoleConfigId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentRole = await _context.EmployeeRoles
                .Include(er => er.EmployeeRoleConfig)
                .FirstOrDefaultAsync(er => er.Id == employeeRoleId);

            if (currentRole == null)
            {
                return Json(new { success = false, message = "Current role not found." });
            }

            var newRoleConfig = await _context.EmployeeRoleConfigs.FindAsync(newRoleConfigId);
            if (newRoleConfig == null)
            {
                return Json(new { success = false, message = "New role configuration not found." });
            }

            // End the current role
            currentRole.ToDate = DateTime.Now;
            currentRole.IsActive = false;
            currentRole.UpdatedBy = currentUser?.UserName;
            currentRole.UpdatedAt = DateTime.Now;

            // Create new role
            var newRole = new EmployeeRole
            {
                EmployeeId = currentRole.EmployeeId,
                EmployeeRoleConfigId = newRoleConfigId,
                FromDate = DateTime.Now,
                IsActive = true,
                CreatedBy = currentUser?.UserName,
                CreatedAt = DateTime.Now,
                CampusId = currentRole.CampusId
            };
            _context.EmployeeRoles.Add(newRole);

            // Handle leave balance adjustment
            await AdjustLeaveBalancesForRoleChange(currentRole.EmployeeId, currentRole.EmployeeRoleConfig, newRoleConfig, currentUser?.UserName);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Role changed successfully with leave balance adjustments!" });
        }

        // Helper method to adjust leave balances when role changes
        private async Task AdjustLeaveBalancesForRoleChange(int employeeId, EmployeeRoleConfig oldRoleConfig, EmployeeRoleConfig newRoleConfig, string? userName)
        {
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            // Get old role leave configs
            var oldLeaveConfigs = await _context.LeaveConfigs
                .Where(lc => lc.IsActive && 
                            (lc.EmployeeType == oldRoleConfig.EmployeeType && 
                            (string.IsNullOrEmpty(lc.RoleName) || lc.RoleName == oldRoleConfig.RoleName)))
                .ToListAsync();

            // Get new role leave configs
            var newLeaveConfigs = await _context.LeaveConfigs
                .Where(lc => lc.IsActive && 
                            (lc.EmployeeType == newRoleConfig.EmployeeType && 
                            (string.IsNullOrEmpty(lc.RoleName) || lc.RoleName == newRoleConfig.RoleName)))
                .ToListAsync();

            var employee = await _context.Employees.FindAsync(employeeId);
            var campusId = employee?.CampusId ?? 0;

            // Process each leave type from new role
            foreach (var newConfig in newLeaveConfigs)
            {
                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && 
                                              lb.LeaveType == newConfig.LeaveType && 
                                              lb.Year == currentYear &&
                                              (newConfig.AllocationPeriod == "Yearly" || lb.Month == currentMonth));

                if (leaveBalance == null)
                {
                    // Create new leave balance
                    leaveBalance = new LeaveBalance
                    {
                        EmployeeId = employeeId,
                        LeaveType = newConfig.LeaveType,
                        Year = currentYear,
                        Month = newConfig.AllocationPeriod == "Monthly" ? currentMonth : null,
                        TotalAllocated = newConfig.AllowedDays,
                        Used = 0,
                        CarriedForward = 0,
                        CreatedBy = userName,
                        CreatedAt = DateTime.Now,
                        CampusId = campusId
                    };
                    _context.LeaveBalances.Add(leaveBalance);

                    // Record history
                    _context.LeaveBalanceHistories.Add(new LeaveBalanceHistory
                    {
                        EmployeeId = employeeId,
                        LeaveType = newConfig.LeaveType,
                        ActionType = "RoleChange",
                        Amount = newConfig.AllowedDays,
                        BalanceBefore = 0,
                        BalanceAfter = newConfig.AllowedDays,
                        Remarks = $"Initial allocation due to role change to {newRoleConfig.RoleName}",
                        CreatedBy = userName,
                        CreatedAt = DateTime.Now,
                        CampusId = campusId
                    });
                }
                else
                {
                    // Adjust existing balance
                    var oldConfig = oldLeaveConfigs.FirstOrDefault(lc => lc.LeaveType == newConfig.LeaveType);
                    if (oldConfig != null && oldConfig.AllowedDays != newConfig.AllowedDays)
                    {
                        var balanceBefore = leaveBalance.Available;
                        var difference = newConfig.AllowedDays - oldConfig.AllowedDays;
                        leaveBalance.TotalAllocated = newConfig.AllowedDays;
                        leaveBalance.UpdatedBy = userName;
                        leaveBalance.UpdatedAt = DateTime.Now;

                        // Record history
                        _context.LeaveBalanceHistories.Add(new LeaveBalanceHistory
                        {
                            EmployeeId = employeeId,
                            LeaveType = newConfig.LeaveType,
                            ActionType = "RoleChange",
                            Amount = difference,
                            BalanceBefore = balanceBefore,
                            BalanceAfter = leaveBalance.Available,
                            Remarks = $"Allocation adjusted from {oldConfig.AllowedDays} to {newConfig.AllowedDays} days due to role change to {newRoleConfig.RoleName}",
                            CreatedBy = userName,
                            CreatedAt = DateTime.Now,
                            CampusId = campusId
                        });
                    }
                }
            }
        }

        // POST: EmployeeRoles/EndRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndRole(int id)
        {
            var employeeRole = await _context.EmployeeRoles.FindAsync(id);
            if (employeeRole == null)
            {
                return NotFound();
            }

            if (employeeRole.ToDate != null)
            {
                TempData["ErrorMessage"] = "This role has already been ended.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            employeeRole.ToDate = DateTime.Now;
            employeeRole.IsActive = false;
            employeeRole.UpdatedBy = currentUser?.UserName;
            employeeRole.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Employee role ended successfully!";
            return RedirectToAction(nameof(Index), new { employeeId = employeeRole.EmployeeId });
        }

        private bool EmployeeRoleExists(int id)
        {
            return _context.EmployeeRoles.Any(e => e.Id == id);
        }
    }
}
