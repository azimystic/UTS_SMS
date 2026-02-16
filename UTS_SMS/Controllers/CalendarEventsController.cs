using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    public class CalendarEventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CalendarEventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: CalendarEvents/GetEvents
        [HttpGet]
        public async Task<IActionResult> GetEvents(int? campusId, int? year, int? month)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            
            // Determine effective campus ID
            int? effectiveCampusId = User.IsInRole("Owner") ? campusId : userCampusId;
            
            var query = _context.CalendarEvents.Where(e => e.IsActive);
            
            if (effectiveCampusId.HasValue && effectiveCampusId > 0)
            {
                query = query.Where(e => e.CampusId == effectiveCampusId);
            }
            
            if (year.HasValue && month.HasValue)
            {
                var startDate = new DateTime(year.Value, month.Value, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                query = query.Where(e => 
                    (e.StartDate >= startDate && e.StartDate <= endDate) ||
                    (e.EndDate.HasValue && e.EndDate >= startDate && e.EndDate <= endDate) ||
                    (e.StartDate <= startDate && (!e.EndDate.HasValue || e.EndDate >= endDate))
                );
            }
            
            var events = await query
                .OrderBy(e => e.StartDate)
                .Select(e => new
                {
                    id = e.Id,
                    title = e.EventName,
                    description = e.Description,
                    startDate = e.StartDate.ToString("yyyy-MM-dd"),
                    endDate = e.EndDate.HasValue ? e.EndDate.Value.ToString("yyyy-MM-dd") : null,
                    isHoliday = e.IsHoliday
                })
                .ToListAsync();
            
            return Json(events);
        }

        // GET: CalendarEvents/GetEventsForDate
        [HttpGet]
        public async Task<IActionResult> GetEventsForDate(int? campusId, DateTime date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            
            int? effectiveCampusId = User.IsInRole("Owner") ? campusId : userCampusId;
            
            var query = _context.CalendarEvents.Where(e => e.IsActive);
            
            if (effectiveCampusId.HasValue && effectiveCampusId > 0)
            {
                query = query.Where(e => e.CampusId == effectiveCampusId);
            }
            
            // Get events for specific date
            query = query.Where(e => 
                (e.StartDate.Date == date.Date) ||
                (e.EndDate.HasValue && e.StartDate.Date <= date.Date && e.EndDate.Value.Date >= date.Date) ||
                (!e.EndDate.HasValue && e.StartDate.Date == date.Date)
            );
            
            // Also get exam dates for this date
            var examDates = await _context.ExamDateSheets
                .Where(ed => ed.ExamDate.Date == date.Date && ed.IsActive)
                .Include(ed => ed.Exam)
                .Include(ed => ed.Subject)
                .Select(ed => new
                {
                    id = ed.Id,
                    title = $"{ed.Exam.Name} - {ed.Subject.Name}",
                    description = $"Exam: {ed.Exam.Name}",
                    startDate = ed.ExamDate.ToString("yyyy-MM-dd"),
                    endDate = (string)null,
                    isHoliday = false,
                    isExam = true
                })
                .ToListAsync();
            
            var events = await query
                .Select(e => new
                {
                    id = e.Id,
                    title = e.EventName,
                    description = e.Description,
                    startDate = e.StartDate.ToString("yyyy-MM-dd"),
                    endDate = e.EndDate.HasValue ? e.EndDate.Value.ToString("yyyy-MM-dd") : null,
                    isHoliday = e.IsHoliday,
                    isExam = false
                })
                .ToListAsync();
            
            var allEvents = events.Concat(examDates).ToList();
            
            return Json(allEvents);
        }

        // POST: CalendarEvents/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CalendarEventCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            
            // Determine campus ID based on role
            int campusId;
            if (User.IsInRole("Owner"))
            {
                campusId = model.CampusId;
            }
            else if (userCampusId.HasValue)
            {
                campusId = userCampusId.Value;
            }
            else
            {
                return BadRequest(new { success = false, message = "User is not associated with any campus" });
            }
            
            var calendarEvent = new CalendarEvent
            {
                CampusId = campusId,
                EventName = model.EventName,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.IsRange ? model.EndDate : null,
                IsHoliday = model.IsHoliday,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name
            };
            
            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Event created successfully", eventId = calendarEvent.Id });
        }

        // POST: CalendarEvents/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            
            if (calendarEvent == null)
            {
                return NotFound();
            }
            
            // Soft delete
            calendarEvent.IsActive = false;
            calendarEvent.UpdatedAt = DateTime.Now;
            calendarEvent.UpdatedBy = User.Identity?.Name;
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Event deleted successfully" });
        }
    }

    public class CalendarEventCreateModel
    {
        public int CampusId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsRange { get; set; }
        public bool IsHoliday { get; set; }
    }
}
