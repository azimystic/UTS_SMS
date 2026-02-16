using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    public class ClassesController : BaseDefinitionController<Class>
    {
        private new readonly ApplicationDbContext _context;

        public ClassesController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
           : base(context, userService, env, userManager) // Add this line
        {
            _context = context;
        }

        // Override to include Campus data
        public override async Task<IActionResult> Index()
        {
            var currentUser = _userManager.GetUserAsync(User).Result; // sync wait
            var campusId = currentUser?.CampusId;
            List<Class> classes;
            if (campusId == null || campusId == 0)
            {
                  classes = await _context.Classes
                .Where(c => c.IsActive)
                .Include(c => c.Campus)
                .ToListAsync();
            }
            else
            {
                  classes = await _context.Classes
                 .Where(c => c.IsActive && c.CampusId == campusId)
                 .Include(c => c.Campus)
                 .ToListAsync();
            }
            
            return View(classes);
        }

        // Override to populate campus dropdown
        public override IActionResult CreateAsync()
        {
            var currentUser = _userManager.GetUserAsync(User).Result; // sync wait
            var campusId = currentUser?.CampusId;

            if (campusId == null || campusId == 0)
            {
                ViewData["ClassTeacherId"] = new SelectList(_context.Employees.Where(c => c.IsActive && c.Role == "Teacher" ), "Id", "FullName");

                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive),
                    "Id", "Name"
                );
            }
            else
            {
                ViewData["ClassTeacherId"] = new SelectList(_context.Employees.Where(c => c.IsActive && c.Role == "Teacher" && c.CampusId == campusId), "Id", "FullName");
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId),
                    "Id", "Name"
                );
            }
            
            return base.CreateAsync();
        }

        // Override to populate campus dropdown
        public override async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes.FindAsync(id);
            if (@class == null || !@class.IsActive)
            {
                return NotFound();
            }
            var currentUser = _userManager.GetUserAsync(User).Result; // sync wait
            var campusId = currentUser?.CampusId;

            if (campusId == null || campusId == 0)
            {
                ViewData["ClassTeacherId"] = new SelectList(_context.Employees.Where(c => c.IsActive && c.Role == "Teacher"), "Id", "FullName");

                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive),
                    "Id", "Name"
                );
            }
            else
            {
                ViewData["ClassTeacherId"] = new SelectList(_context.Employees.Where(c => c.IsActive && c.Role == "Teacher" && c.CampusId == campusId), "Id", "FullName");
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId),
                    "Id", "Name"
                );
            }
            return View(@class);
        }
    }

}
