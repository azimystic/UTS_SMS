using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    public class AdminNotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminNotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminNotifications
        public async Task<IActionResult> Index(string filterAction = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var query = _context.AdminNotifications
                .Include(an => an.Student)
                .Include(an => an.PickupCard)
                .Include(an => an.Campus)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filterAction))
            {
                query = query.Where(an => an.Action.Contains(filterAction));
            }

            if (startDate.HasValue)
            {
                query = query.Where(an => an.ActionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(an => an.ActionDate <= endDate.Value.AddDays(1));
            }

            // Order by most recent first
            query = query.OrderByDescending(an => an.ActionDate);

            // Pagination
            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.FilterAction = filterAction;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            // Get unique action types for filter dropdown
            ViewBag.ActionTypes = await _context.AdminNotifications
                .Select(an => an.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return View(notifications);
        }

        // POST: Mark notification as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.AdminNotifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // POST: Mark all as read
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unreadNotifications = await _context.AdminNotifications
                .Where(an => !an.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, count = unreadNotifications.Count });
        }

        // GET: Get unread count for badge
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _context.AdminNotifications
                .Where(an => !an.IsRead)
                .CountAsync();

            return Json(new { count });
        }
    }
}
