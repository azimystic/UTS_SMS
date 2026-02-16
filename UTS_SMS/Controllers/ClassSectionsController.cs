using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{

    public class ClassSectionsController : BaseDefinitionController<ClassSection>
    {
        private readonly ApplicationDbContext _context;

        public ClassSectionsController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
            : base(context, userService, env, userManager) // Add this line
        {
            _context = context;
        }

        // Override to include Class data
        public override async Task<IActionResult> Index()
        {
            var currentUser = _userManager.GetUserAsync(User).Result; // sync wait
            var campusId = currentUser?.CampusId;
            List<ClassSection> sections;
            if (campusId == null || campusId == 0)
            {
                  sections = await _context.ClassSections
               .Where(cs => cs.IsActive)
               .Include(cs => cs.Class)
               .OrderBy(cs => cs.Class.GradeLevel)
               .ThenBy(cs => cs.Name)
               .ToListAsync();
            }
            else
            {
                  sections = await _context.ClassSections
                .Where(cs => cs.IsActive && cs.CampusId == campusId)
                .Include(cs => cs.Class)
                .OrderBy(cs => cs.Class.GradeLevel)
                .ThenBy(cs => cs.Name)
                .ToListAsync();
            }

            // Group sections by class
            var groupedSections = sections
                .GroupBy(s => new { s.ClassId, s.Class.Name, s.Class.GradeLevel })
                .OrderBy(g => g.Key.GradeLevel)
                .ToList();

            ViewBag.GroupedSections = groupedSections;
            return View(sections);
        }
        public override IActionResult CreateAsync()
        {
            var currentUser = _userManager.GetUserAsync(User).Result; // sync wait
            var campusId = currentUser?.CampusId;

            if (campusId == null || campusId == 0)
            {
                ViewData["ClassId"] = new SelectList(
                    _context.Classes.Where(c => c.IsActive),
                    "Id", "Name"
                );
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive),
                    "Id", "Name"
                );
            }
            else
            {
                ViewData["ClassId"] = new SelectList(
                    _context.Classes.Where(cs => cs.IsActive && cs.CampusId == campusId),
                    "Id", "Name"
                );
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId),
                    "Id", "Name"
                );
            }

            return View();
        }



        // Override to populate class dropdown
        public override async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.ClassSections.FindAsync(id);
            if (section == null || !section.IsActive)
            {
                return NotFound();
            }
            var currentUser = _userManager.GetUserAsync(User).Result; // sync wait
            var campusId = currentUser?.CampusId;

            if (campusId == null || campusId == 0)
            {
                ViewData["ClassId"] = new SelectList(
                    _context.Classes.Where(c => c.IsActive),
                    "Id", "Name"
                );
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive),
                    "Id", "Name"
                );
            }
            else
            {
                ViewData["ClassId"] = new SelectList(
                    _context.Classes.Where(cs => cs.IsActive && cs.CampusId == campusId),
                    "Id", "Name"
                );
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId),
                    "Id", "Name"
                );
            }
            return View(section);
        }
    }

}
