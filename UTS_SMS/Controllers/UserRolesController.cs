using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Owner,Admin")]
    public class UserRolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        // GET: UserRoles - Main page to manage role-menu assignments
        public async Task<IActionResult> Index(string? selectedRole)
        {
            // Get all roles
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewBag.Roles = roles;
            ViewBag.SelectedRole = selectedRole;

            if (!string.IsNullOrEmpty(selectedRole))
            {
                // Get all parent menus with children
                var parentMenus = await _context.AppMenuParents
                    .Include(p => p.Children.OrderBy(c => c.DisplayOrder))
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();

                // Get existing role assignments for this role
                var roleAssignments = await _context.AppMenuRoleAssignments
                    .Where(ra => ra.RoleName == selectedRole)
                    .ToListAsync();

                // Create lookup for fast access
                var assignedParentIds = roleAssignments
                    .Where(ra => ra.ParentMenuId.HasValue)
                    .Select(ra => ra.ParentMenuId.Value)
                    .ToHashSet();

                var assignedChildIds = roleAssignments
                    .Where(ra => ra.ChildMenuId.HasValue)
                    .Select(ra => ra.ChildMenuId.Value)
                    .ToHashSet();

                ViewBag.AssignedParentIds = assignedParentIds;
                ViewBag.AssignedChildIds = assignedChildIds;

                return View(parentMenus);
            }

            return View(new List<AppMenuParent>());
        }

        // POST: UserRoles/AssignMenus - Save role-menu assignments
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMenus(string roleName, List<int> parentMenuIds, List<int> childMenuIds)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                TempData["ErrorMessage"] = "Role name is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Remove all existing assignments for this role
                var existingAssignments = await _context.AppMenuRoleAssignments
                    .Where(ra => ra.RoleName == roleName)
                    .ToListAsync();

                _context.AppMenuRoleAssignments.RemoveRange(existingAssignments);

                // Add new parent menu assignments
                if (parentMenuIds != null && parentMenuIds.Any())
                {
                    foreach (var parentId in parentMenuIds)
                    {
                        _context.AppMenuRoleAssignments.Add(new AppMenuRoleAssignment
                        {
                            RoleName = roleName,
                            ParentMenuId = parentId,
                            CreatedBy = User.Identity?.Name ?? "System",
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                // Add new child menu assignments
                if (childMenuIds != null && childMenuIds.Any())
                {
                    foreach (var childId in childMenuIds)
                    {
                        _context.AppMenuRoleAssignments.Add(new AppMenuRoleAssignment
                        {
                            RoleName = roleName,
                            ChildMenuId = childId,
                            CreatedBy = User.Identity?.Name ?? "System",
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Menu assignments for role '{roleName}' have been updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving assignments: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { selectedRole = roleName });
        }
    }
}
