using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;
using System.Data;

namespace SMS.Controllers
{
    // Base Controller for common definition operations
    public class BaseDefinitionController<T> : Controller where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IUserService _userService;
        protected readonly IWebHostEnvironment _env;
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseDefinitionController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
        }

        // GET: List all items
        public virtual async Task<IActionResult> Index()
        {
            // 1. Get the current user
            var currentUser = await _userManager.GetUserAsync(User);

            // 2. Retrieve the current user's CampusId
            var userCampusId = currentUser.CampusId;

            // 3. Start building the query
            var query = _context.Set<T>()
                .Where(x => EF.Property<bool>(x, "IsActive")); // Filter by IsActive

            // 4. Add the CampusId filter
         
            query = query.Where(x => EF.Property<int>(x, "CampusId") == userCampusId);

            // 5. Execute the query and return the view
            return View(await query.ToListAsync());
        }

        // GET: Details of specific item
        public virtual async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Set<T>().FindAsync(id);
            if (item == null || !_context.Entry(item).Property<bool>("IsActive").CurrentValue)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Create form
        public virtual IActionResult CreateAsync()
        {
            return View();
        }

        // POST: Create new item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Create(T item)
        {
             ModelState.Remove("Classes");
            ModelState.Remove("Campus");
            ModelState.Remove("ClassSections");
            ModelState.Remove("TeacherAssignments");
            ModelState.Remove("Class");
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Edit form
        public virtual async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Set<T>().FindAsync(id);
            if (item == null || !_context.Entry(item).Property<bool>("IsActive").CurrentValue)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Update item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Edit(int id, T item)
        {
            if (id != (int)item.GetType().GetProperty("Id").GetValue(item))
            {
                return NotFound();
            }
            ModelState.Remove("Class");
            ModelState.Remove("Classes");
            ModelState.Remove("Campus");
            ModelState.Remove("ClassSections");
            ModelState.Remove("TeacherAssignments");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ItemExists((int)item.GetType().GetProperty("Id").GetValue(item)))
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
            return View(item);
        }

        // GET: Delete confirmation
        public virtual async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Set<T>().FindAsync(id);
            if (item == null || !_context.Entry(item).Property<bool>("IsActive").CurrentValue)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Soft delete item
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Set<T>().FindAsync(id);
            if (item != null)
            {
                item.GetType().GetProperty("IsActive").SetValue(item, false);
                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ItemExists(int id)
        {
            return await _context.Set<T>().AnyAsync(e =>
                (int)e.GetType().GetProperty("Id").GetValue(e) == id &&
                EF.Property<bool>(e, "IsActive"));
        }
    }
     
}
