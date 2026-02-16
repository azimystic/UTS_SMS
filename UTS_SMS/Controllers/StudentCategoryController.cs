using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentCategoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentCategory
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var categories = _context.StudentCategories
                .Include(sc => sc.EmployeeCategoryDiscounts)
                .AsQueryable();

            if (campusId.HasValue && campusId.Value > 0)
            {
                categories = categories.Where(sc => sc.CampusId == campusId);
            }

            return View(await categories.Where(sc => sc.IsActive).OrderBy(sc => sc.CategoryName).ToListAsync());
        }

        // GET: StudentCategory/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name");
            }
            else
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            }
            
            return View();
        }

        // POST: StudentCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentCategory category, List<EmployeeCategoryDiscount>? discounts)
        {
            ModelState.Remove("Campus");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            var keysToRemove = ModelState.Keys
    .Where(k => k.Contains("StudentCategory"))
    .ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
            if (ModelState.IsValid)
            {
                try
                {
                    category.CreatedDate = DateTime.Now;
                    category.CreatedBy = User.Identity?.Name;
                    category.IsActive = true;
                    category.ModifiedDate = DateTime.Now;
                    category.ModifiedBy = User.Identity?.Name;
                    _context.StudentCategories.Add(category);
                    await _context.SaveChangesAsync();

                    if (discounts != null && discounts.Any())
                    {
                        foreach (var discount in discounts)
                        {
                            discount.StudentCategoryId = category.Id;
                            _context.EmployeeCategoryDiscounts.Add(discount);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Student Category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name");
            }
            else
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            }
            
            return View(category);
        }

        // GET: StudentCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.StudentCategories
                .Include(sc => sc.EmployeeCategoryDiscounts)
                .FirstOrDefaultAsync(sc => sc.Id == id);
                
            if (category == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", category.CampusId);
            }
            else
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", category.CampusId);
            }
            
            return View(category);
        }

        // POST: StudentCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentCategory category, List<EmployeeCategoryDiscount>? discounts)
        {
            if (id != category.Id)
            {
                return NotFound();
            }
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("Campus");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.StudentCategories
                        .Include(sc => sc.EmployeeCategoryDiscounts)
                        .FirstOrDefaultAsync(sc => sc.Id == id);

                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.CategoryName = category.CategoryName;
                    existing.CategoryType = category.CategoryType;
                     existing.DefaultAdmissionFeeDiscount = category.DefaultAdmissionFeeDiscount;
                    existing.DefaultTuitionFeeDiscount = category.DefaultTuitionFeeDiscount;
                    existing.SiblingCount = category.SiblingCount;
                    existing.PerSiblingAdmissionDiscount = category.PerSiblingAdmissionDiscount;
                    existing.PerSiblingTuitionDiscount = category.PerSiblingTuitionDiscount;
                    existing.CampusId = category.CampusId;
                    category.ModifiedDate = DateTime.Now;
                    category.ModifiedBy = User.Identity?.Name;
                    _context.EmployeeCategoryDiscounts.RemoveRange(existing.EmployeeCategoryDiscounts);

                    if (discounts != null && discounts.Any())
                    {
                        foreach (var discount in discounts)
                        {
                            discount.StudentCategoryId = existing.Id;
                            _context.EmployeeCategoryDiscounts.Add(discount);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Student Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", category.CampusId);
            }
            else
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", category.CampusId);
            }
            
            return View(category);
        }

        // POST: StudentCategory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.StudentCategories.FindAsync(id);
            
            if (category == null)
            {
                return NotFound();
            }

            if (category.IsSystemDefined)
            {
                TempData["ErrorMessage"] = "Cannot delete system-defined categories!";
                return RedirectToAction(nameof(Index));
            }

            category.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student Category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.StudentCategories.Any(e => e.Id == id);
        }

        // API: Get categories by campus
        [HttpGet]
        public async Task<JsonResult> GetCategoriesByCampus(int? campusId)
        {
            var query = _context.StudentCategories.Where(sc => sc.IsActive);

            if (campusId.HasValue && campusId.Value > 0)
            {
                query = query.Where(sc => sc.CampusId == campusId);
            }

            var categories = await query
                .Select(sc => new
                {
                    id = sc.Id,
                    name = sc.CategoryName,
                    type = sc.CategoryType,
                    admissionDiscount = sc.DefaultAdmissionFeeDiscount,
                    tuitionDiscount = sc.DefaultTuitionFeeDiscount
                })
                .OrderBy(sc => sc.name)
                .ToListAsync();

            return Json(categories);
        }
        
        // API: Get category details with all info
        [HttpGet]
        public async Task<JsonResult> GetCategoryDetails(int categoryId)
        {
            var category = await _context.StudentCategories
                .Include(sc => sc.EmployeeCategoryDiscounts)
                .FirstOrDefaultAsync(sc => sc.Id == categoryId);
                
            if (category == null)
            {
                return Json(new { success = false });
            }
            
            var result = new
            {
                success = true,
                categoryName = category.CategoryName,
                categoryType = category.CategoryType,
                defaultAdmissionDiscount = category.DefaultAdmissionFeeDiscount,
                defaultTuitionDiscount = category.DefaultTuitionFeeDiscount,
                siblingCount = category.SiblingCount,
                perSiblingAdmissionDiscount = category.PerSiblingAdmissionDiscount,
                perSiblingTuitionDiscount = category.PerSiblingTuitionDiscount,
                employeeDiscounts = category.EmployeeCategoryDiscounts.Select(ed => new
                {
                    role = ed.EmployeeCategory,
                    admissionDiscount = ed.AdmissionFeeDiscount,
                    tuitionDiscount = ed.TuitionFeeDiscount
                }).ToList()
            };
            
            return Json(result);
        }
    }
}
