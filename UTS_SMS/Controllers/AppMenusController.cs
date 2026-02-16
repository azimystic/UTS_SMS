using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Owner,Admin")]
    public class AppMenusController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppMenusController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AppMenus - List all parent menus
        public async Task<IActionResult> Index()
        {
            var parentMenus = await _context.AppMenuParents
                .Include(p => p.Children)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
            return View(parentMenus);
        }

        // GET: AppMenus/CreateParent
        public IActionResult CreateParent()
        {
            return View();
        }

        // POST: AppMenus/CreateParent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParent(AppMenuParent parent)
        {
            if (ModelState.IsValid)
            {
                parent.CreatedBy = User.Identity?.Name ?? "System";
                parent.CreatedDate = DateTime.Now;
                _context.AppMenuParents.Add(parent);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Parent menu created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(parent);
        }

        // GET: AppMenus/EditParent/5
        public async Task<IActionResult> EditParent(int? id)
        {
            if (id == null) return NotFound();

            var parent = await _context.AppMenuParents.FindAsync(id);
            if (parent == null) return NotFound();

            return View(parent);
        }

        // POST: AppMenus/EditParent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParent(int id, AppMenuParent parent)
        {
            if (id != parent.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingParent = await _context.AppMenuParents.FindAsync(id);
                    if (existingParent == null) return NotFound();

                    existingParent.ControllerName = parent.ControllerName;
                    existingParent.DisplayName = parent.DisplayName;
                    existingParent.IconClass = parent.IconClass;
                    existingParent.IsClickable = parent.IsClickable;
                    existingParent.HasChildren = parent.HasChildren;
                    existingParent.DisplayOrder = parent.DisplayOrder;
                    existingParent.IsActive = parent.IsActive;
                    existingParent.AllowedRoles = parent.AllowedRoles;
                    existingParent.ModifiedBy = User.Identity?.Name ?? "System";
                    existingParent.ModifiedDate = DateTime.Now;

                    _context.Update(existingParent);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Parent menu updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParentMenuExists(parent.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(parent);
        }

        // POST: AppMenus/DeleteParent/5
        [HttpPost]
        public async Task<IActionResult> DeleteParent(int id)
        {
            var parent = await _context.AppMenuParents
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (parent == null)
                return Json(new { success = false, message = "Parent menu not found" });

            try
            {
                _context.AppMenuParents.Remove(parent);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Parent menu deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: AppMenus/ManageChildren/5 - Manage children for a specific parent
        public async Task<IActionResult> ManageChildren(int id)
        {
            var parent = await _context.AppMenuParents
                .Include(p => p.Children.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parent == null) return NotFound();

            ViewBag.ParentId = id;
            ViewBag.ParentName = parent.DisplayName;
            return View(parent);
        }

        // GET: AppMenus/CreateChild/5
        public async Task<IActionResult> CreateChild(int parentId)
        {
            var parent = await _context.AppMenuParents.FindAsync(parentId);
            if (parent == null) return NotFound();

            ViewBag.ParentId = parentId;
            ViewBag.ParentName = parent.DisplayName;
            ViewBag.ControllerName = parent.ControllerName;
            
            return View(new AppMenuChild { ParentMenuId = parentId });
        }

        // POST: AppMenus/CreateChild
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChild(AppMenuChild child)
        {
            if (ModelState.IsValid)
            {
                child.CreatedBy = User.Identity?.Name ?? "System";
                child.CreatedDate = DateTime.Now;
                
                // Auto-generate URL if not provided
                if (string.IsNullOrEmpty(child.Url))
                {
                    var parent = await _context.AppMenuParents.FindAsync(child.ParentMenuId);
                    if (parent != null)
                    {
                        child.Url = $"/{parent.ControllerName}/{child.ActionName}";
                    }
                }
                
                _context.AppMenuChildren.Add(child);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Child menu created successfully!";
                return RedirectToAction(nameof(ManageChildren), new { id = child.ParentMenuId });
            }
            
            var parentMenu = await _context.AppMenuParents.FindAsync(child.ParentMenuId);
            ViewBag.ParentId = child.ParentMenuId;
            ViewBag.ParentName = parentMenu?.DisplayName;
            ViewBag.ControllerName = parentMenu?.ControllerName;
            return View(child);
        }

        // GET: AppMenus/EditChild/5
        public async Task<IActionResult> EditChild(int? id)
        {
            if (id == null) return NotFound();

            var child = await _context.AppMenuChildren
                .Include(c => c.ParentMenu)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (child == null) return NotFound();

            ViewBag.ParentId = child.ParentMenuId;
            ViewBag.ParentName = child.ParentMenu?.DisplayName;
            ViewBag.ControllerName = child.ParentMenu?.ControllerName;
            
            return View(child);
        }

        // POST: AppMenus/EditChild/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChild(int id, AppMenuChild child)
        {
            if (id != child.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingChild = await _context.AppMenuChildren.FindAsync(id);
                    if (existingChild == null) return NotFound();

                    existingChild.ActionName = child.ActionName;
                    existingChild.DisplayName = child.DisplayName;
                    existingChild.IconClass = child.IconClass;
                    existingChild.Url = child.Url;
                    existingChild.IsIncluded = child.IsIncluded;
                    existingChild.DisplayOrder = child.DisplayOrder;
                    existingChild.IsActive = child.IsActive;
                    existingChild.AllowedRoles = child.AllowedRoles;
                    existingChild.ModifiedBy = User.Identity?.Name ?? "System";
                    existingChild.ModifiedDate = DateTime.Now;

                    // Auto-generate URL if not provided
                    if (string.IsNullOrEmpty(existingChild.Url))
                    {
                        var parent = await _context.AppMenuParents.FindAsync(existingChild.ParentMenuId);
                        if (parent != null)
                        {
                            existingChild.Url = $"/{parent.ControllerName}/{existingChild.ActionName}";
                        }
                    }

                    _context.Update(existingChild);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Child menu updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChildMenuExists(child.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(ManageChildren), new { id = child.ParentMenuId });
            }
            
            var parentMenu = await _context.AppMenuParents.FindAsync(child.ParentMenuId);
            ViewBag.ParentId = child.ParentMenuId;
            ViewBag.ParentName = parentMenu?.DisplayName;
            ViewBag.ControllerName = parentMenu?.ControllerName;
            return View(child);
        }

        // POST: AppMenus/DeleteChild/5
        [HttpPost]
        public async Task<IActionResult> DeleteChild(int id)
        {
            var child = await _context.AppMenuChildren.FindAsync(id);
            
            if (child == null)
                return Json(new { success = false, message = "Child menu not found" });

            try
            {
                _context.AppMenuChildren.Remove(child);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Child menu deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AppMenus/ReorderParents
        [HttpPost]
        public async Task<IActionResult> ReorderParents([FromBody] List<int> orderedIds)
        {
            try
            {
                for (int i = 0; i < orderedIds.Count; i++)
                {
                    var parent = await _context.AppMenuParents.FindAsync(orderedIds[i]);
                    if (parent != null)
                    {
                        parent.DisplayOrder = i + 1;
                        parent.ModifiedBy = User.Identity?.Name ?? "System";
                        parent.ModifiedDate = DateTime.Now;
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Menu order updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AppMenus/ReorderChildren
        [HttpPost]
        public async Task<IActionResult> ReorderChildren([FromBody] List<int> orderedIds)
        {
            try
            {
                for (int i = 0; i < orderedIds.Count; i++)
                {
                    var child = await _context.AppMenuChildren.FindAsync(orderedIds[i]);
                    if (child != null)
                    {
                        child.DisplayOrder = i + 1;
                        child.ModifiedBy = User.Identity?.Name ?? "System";
                        child.ModifiedDate = DateTime.Now;
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Menu order updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool ParentMenuExists(int id)
        {
            return _context.AppMenuParents.Any(e => e.Id == id);
        }

        private bool ChildMenuExists(int id)
        {
            return _context.AppMenuChildren.Any(e => e.Id == id);
        }
    }
}
