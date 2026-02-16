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
    public class EmployeeRolesConfigController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public EmployeeRolesConfigController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        // GET: EmployeeRolesConfig
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var configs = campusId == null || campusId == 0
                ? await _context.EmployeeRoleConfigs
                    .Where(c => c.IsActive)
                    .Include(c => c.Campus)
                    .OrderBy(c => c.EmployeeType)
                    .ThenBy(c => c.RoleName)
                    .ToListAsync()
                : await _context.EmployeeRoleConfigs
                    .Where(c => c.IsActive && c.CampusId == campusId)
                    .Include(c => c.Campus)
                    .OrderBy(c => c.EmployeeType)
                    .ThenBy(c => c.RoleName)
                    .ToListAsync();

            return View(configs);
        }

        // GET: EmployeeRolesConfig/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CampusId"] = campusId == null || campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name")
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name");

            ViewData["EmployeeTypes"] = new SelectList(new[]
            {
                "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor"
            });

            return View();
        }

        // POST: EmployeeRolesConfig/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleName,EmployeeType,Description,CampusId")] EmployeeRoleConfig employeeRoleConfig)
        {
            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                employeeRoleConfig.CreatedBy = currentUser?.UserName;
                employeeRoleConfig.CreatedAt = DateTime.Now;
                
                _context.Add(employeeRoleConfig);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Role configuration created successfully!";
                return RedirectToAction(nameof(Index));
            }

            var campusId = employeeRoleConfig.CampusId;
            ViewData["CampusId"] = campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name")
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name");

            ViewData["EmployeeTypes"] = new SelectList(new[]
            {
                "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor"
            });

            return View(employeeRoleConfig);
        }

        // GET: EmployeeRolesConfig/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeRoleConfig = await _context.EmployeeRoleConfigs.FindAsync(id);
            if (employeeRoleConfig == null || !employeeRoleConfig.IsActive)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CampusId"] = campusId == null || campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", employeeRoleConfig.CampusId)
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name", employeeRoleConfig.CampusId);

            ViewData["EmployeeTypes"] = new SelectList(new[]
            {
                "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor"
            }, employeeRoleConfig.EmployeeType);

            return View(employeeRoleConfig);
        }

        // POST: EmployeeRolesConfig/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoleName,EmployeeType,Description,CampusId,IsActive,CreatedAt,CreatedBy")] EmployeeRoleConfig employeeRoleConfig)
        {
            if (id != employeeRoleConfig.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    employeeRoleConfig.UpdatedBy = currentUser?.UserName;
                    employeeRoleConfig.UpdatedAt = DateTime.Now;
                    
                    _context.Update(employeeRoleConfig);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Role configuration updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeRoleConfigExists(employeeRoleConfig.Id))
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

            var campusId = employeeRoleConfig.CampusId;
            ViewData["CampusId"] = campusId == 0
                ? new SelectList(await _context.Campuses.Where(c => c.IsActive).ToListAsync(), "Id", "Name", employeeRoleConfig.CampusId)
                : new SelectList(await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync(), "Id", "Name", employeeRoleConfig.CampusId);

            ViewData["EmployeeTypes"] = new SelectList(new[]
            {
                "Teacher", "Admin", "Accountant", "Aya", "Guard", "Lab Instructor"
            }, employeeRoleConfig.EmployeeType);

            return View(employeeRoleConfig);
        }

        // POST: EmployeeRolesConfig/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeRoleConfig = await _context.EmployeeRoleConfigs.FindAsync(id);
            if (employeeRoleConfig != null)
            {
                employeeRoleConfig.IsActive = false;
                var currentUser = await _userManager.GetUserAsync(User);
                employeeRoleConfig.UpdatedBy = currentUser?.UserName;
                employeeRoleConfig.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Role configuration deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeRoleConfigExists(int id)
        {
            return _context.EmployeeRoleConfigs.Any(e => e.Id == id);
        }
    }
}
