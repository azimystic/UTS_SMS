using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Services
{
    public class MenuService
    {
        private readonly ApplicationDbContext _context;

        public MenuService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AppMenuParent>> GetMenusForUserAsync(IEnumerable<string> userRoles)
        {
            var rolesList = userRoles.ToList();

            // Get parent menu IDs assigned to user's roles
            var assignedParentIds = await _context.AppMenuRoleAssignments
                .Where(ra => rolesList.Contains(ra.RoleName) && ra.ParentMenuId.HasValue)
                .Select(ra => ra.ParentMenuId!.Value)
                .Distinct()
                .ToListAsync();

            // Get child menu IDs assigned to user's roles
            var assignedChildIds = await _context.AppMenuRoleAssignments
                .Where(ra => rolesList.Contains(ra.RoleName) && ra.ChildMenuId.HasValue)
                .Select(ra => ra.ChildMenuId!.Value)
                .Distinct()
                .ToListAsync();

            // Get all parent menus that are assigned to the user
            var allMenus = await _context.AppMenuParents
                .Include(p => p.Children.Where(c => c.IsActive && c.IsIncluded))
                .Where(p => p.IsActive && assignedParentIds.Contains(p.Id))
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // Filter children to only show those assigned to the user
            foreach (var menu in allMenus)
            {
                menu.Children = menu.Children
                    .Where(c => assignedChildIds.Contains(c.Id))
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();
            }

            return allMenus;
        }

        public async Task<List<AppMenuParent>> SearchMenusAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<AppMenuParent>();

            searchTerm = searchTerm.ToLower();

            var menus = await _context.AppMenuParents
                .Include(p => p.Children.Where(c => c.IsActive && c.IsIncluded))
                .Where(p => p.IsActive && (
                    p.DisplayName.ToLower().Contains(searchTerm) ||
                    p.ControllerName.ToLower().Contains(searchTerm) ||
                    p.Children.Any(c => c.DisplayName.ToLower().Contains(searchTerm) || c.ActionName.ToLower().Contains(searchTerm))
                ))
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // Filter children to only include those matching the search
            foreach (var menu in menus)
            {
                menu.Children = menu.Children
                    .Where(c => c.DisplayName.ToLower().Contains(searchTerm) || 
                               c.ActionName.ToLower().Contains(searchTerm))
                    .ToList();
            }

            return menus;
        }
    }
}
