using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{
    public class BankAccountsController : BaseDefinitionController<BankAccount>
    {
        private new readonly ApplicationDbContext _context;

        public BankAccountsController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
           : base(context, userService, env, userManager) // Add this line
        {
            _context = context;
        }


        // Override to populate class dropdown
        public override IActionResult CreateAsync()
        {
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            return base.CreateAsync();
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
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            return View(section);
        }
    }
}
