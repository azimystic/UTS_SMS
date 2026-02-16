using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public NotificationsController(
            NotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Get all notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var role = User.Claims.FirstOrDefault(c => c.Type.Contains("role"))?.Value ?? "User";
                var notifications = await _notificationService.GetNotificationsForUser(
                    currentUser.Id, 
                    role, 
                    currentUser.CampusId ?? 0
                );

                var result = notifications.Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    title = n.Title,
                    message = n.Message,
                    timestamp = n.Timestamp,
                    isRead = n.IsRead,
                    actionUrl = n.ActionUrl
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var role = User.Claims.FirstOrDefault(c => c.Type.Contains("role"))?.Value ?? "User";
                var count = await _notificationService.GetUnreadCount(
                    currentUser.Id, 
                    role, 
                    currentUser.CampusId ?? 0
                );

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                await _notificationService.MarkAsRead(id, currentUser.Id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var role = User.Claims.FirstOrDefault(c => c.Type.Contains("role"))?.Value ?? "User";
                await _notificationService.MarkAllAsRead(
                    currentUser.Id, 
                    role, 
                    currentUser.CampusId ?? 0
                );

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
