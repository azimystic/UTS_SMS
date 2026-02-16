using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    public class SubjectsController : BaseDefinitionController<Subject>
    {
        private new readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public SubjectsController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
            : base(context, userService, env, userManager) // Add this line
        {
            _context = context;
            _userManager = userManager;
        }


        // Override to populate class dropdown
        public override IActionResult CreateAsync()
        {
            var currentUserTask = _userManager.GetUserAsync(User);
            currentUserTask.Wait(); // Synchronously wait for the result (since method is not async)
            var currentUser = currentUserTask.Result;
            var userCampusId = currentUser?.CampusId;
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == userCampusId), "Id", "Name");
            }
            else
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            }
            return base.CreateAsync();
        }

        // Override to populate class dropdown
        public override async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.Subjects.FindAsync(id);
            if (section == null || !section.IsActive)
            {
                return NotFound();
            }

            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Filter campus dropdown based on user role
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == userCampusId), "Id", "Name");
            }
            else
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            }

            return View(section);
        }
    }
}
