using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS;
using UTS_SMS.Models;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AcademicCalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicCalendarController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AcademicCalendar
        public async Task<IActionResult> Index(int? year, int? month, int? campusFilter, string holidayType = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            var currentDate = DateTime.Today;
            var targetYear = year ?? currentDate.Year;
            var targetMonth = month ?? currentDate.Month;

            // Build query for holidays
            var holidaysQuery = _context.AcademicCalendars
                .Include(ac => ac.Campus)
                .Where(ac => ac.IsActive);

            // Apply campus filter
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                holidaysQuery = holidaysQuery.Where(ac => ac.CampusId == userCampusId.Value);
            }
            else if (campusFilter.HasValue)
            {
                holidaysQuery = holidaysQuery.Where(ac => ac.CampusId == campusFilter.Value);
            }

            // Apply holiday type filter
            if (!string.IsNullOrEmpty(holidayType))
            {
                holidaysQuery = holidaysQuery.Where(ac => ac.HolidayType == holidayType);
            }

            var allHolidays = await holidaysQuery
                .OrderBy(ac => ac.Date)
                .ToListAsync();

            // Get holidays for the target month
            var monthlyHolidays = allHolidays
                .Where(h => h.Date.Year == targetYear && h.Date.Month == targetMonth)
                .ToList();

            // Build calendar days
            var firstDayOfMonth = new DateTime(targetYear, targetMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var startDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
            var endDate = lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek);

            var calendarDays = new List<CalendarDay>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayHolidays = allHolidays.Where(h => h.Date.Date == date.Date).ToList();
                calendarDays.Add(new CalendarDay
                {
                    Day = date.Day,
                    IsHoliday = dayHolidays.Any(),
                    Holidays = dayHolidays,
                    IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
                    IsCurrentMonth = date.Month == targetMonth
                });
            }

            var viewModel = new MonthlyCalendarViewModel
            {
                Year = targetYear,
                Month = targetMonth,
                MonthName = firstDayOfMonth.ToString("MMMM yyyy"),
                Holidays = monthlyHolidays,
                CalendarDays = calendarDays
            };

            // Get available campuses for dropdown
            var campusesQuery = _context.Campuses.AsQueryable();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusesQuery
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Campuses = campuses;
            ViewBag.Year = targetYear;
            ViewBag.Month = targetMonth;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.HolidayType = holidayType;
            ViewBag.UserCampusId = userCampusId;
            ViewBag.HolidayTypes = new List<string> { "National", "Religious", "Academic", "Local" };

            return View(viewModel);
        }

        // GET: AcademicCalendar/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            var campusesQuery = _context.Campuses.AsQueryable();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusesQuery
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CampusId = new SelectList(campuses, "Id", "Name");
            ViewBag.HolidayTypes = new SelectList(new List<string> { "National", "Religious", "Academic", "Local" });

            return View();
        }

        // POST: AcademicCalendar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicCalendar academicCalendar)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            // Validate campus access
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                academicCalendar.CampusId = userCampusId.Value;
            }

            // Check for duplicate holiday on same date for same campus
            var existingHoliday = await _context.AcademicCalendars
                .AnyAsync(ac => ac.Date.Date == academicCalendar.Date.Date && 
                               ac.CampusId == academicCalendar.CampusId &&
                               ac.IsActive);

            if (existingHoliday)
            {
                ModelState.AddModelError("Date", "A holiday already exists for this date in the selected campus.");
            }

            ModelState.Remove("Campus");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("UpdatedBy");

            if (ModelState.IsValid)
            {
                academicCalendar.CreatedBy = User.Identity?.Name;
                academicCalendar.CreatedAt = DateTime.Now;
                
                _context.AcademicCalendars.Add(academicCalendar);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Holiday added successfully!";
                return RedirectToAction(nameof(Index), new { 
                    year = academicCalendar.Date.Year, 
                    month = academicCalendar.Date.Month 
                });
            }

            // Re-populate dropdowns
            var campusesQuery = _context.Campuses.AsQueryable();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusesQuery
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CampusId = new SelectList(campuses, "Id", "Name", academicCalendar.CampusId);
            ViewBag.HolidayTypes = new SelectList(new List<string> { "National", "Religious", "Academic", "Local" }, academicCalendar.HolidayType);

            return View(academicCalendar);
        }

        // GET: AcademicCalendar/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var academicCalendar = await _context.AcademicCalendars
                .Include(ac => ac.Campus)
                .FirstOrDefaultAsync(ac => ac.Id == id);

            if (academicCalendar == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            // Check campus access
            if (userCampusId.HasValue && userCampusId.Value > 0 && academicCalendar.CampusId != userCampusId.Value)
                return Forbid();

            var campusesQuery = _context.Campuses.AsQueryable();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusesQuery
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CampusId = new SelectList(campuses, "Id", "Name", academicCalendar.CampusId);
            ViewBag.HolidayTypes = new SelectList(new List<string> { "National", "Religious", "Academic", "Local" }, academicCalendar.HolidayType);

            return View(academicCalendar);
        }

        // POST: AcademicCalendar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicCalendar academicCalendar)
        {
            if (id != academicCalendar.Id)
                return NotFound();

            var existingCalendar = await _context.AcademicCalendars.FindAsync(id);
            if (existingCalendar == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            // Check campus access
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                if (existingCalendar.CampusId != userCampusId.Value)
                    return Forbid();
                academicCalendar.CampusId = userCampusId.Value;
            }

            // Check for duplicate (excluding current record)
            var duplicateHoliday = await _context.AcademicCalendars
                .AnyAsync(ac => ac.Id != id &&
                               ac.Date.Date == academicCalendar.Date.Date && 
                               ac.CampusId == academicCalendar.CampusId &&
                               ac.IsActive);

            if (duplicateHoliday)
            {
                ModelState.AddModelError("Date", "A holiday already exists for this date in the selected campus.");
            }

            ModelState.Remove("Campus");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("UpdatedBy");

            if (ModelState.IsValid)
            {
                existingCalendar.Date = academicCalendar.Date;
                existingCalendar.HolidayName = academicCalendar.HolidayName;
                existingCalendar.Description = academicCalendar.Description;
                existingCalendar.HolidayType = academicCalendar.HolidayType;
                existingCalendar.IsActive = academicCalendar.IsActive;
                existingCalendar.UpdatedAt = DateTime.Now;
                existingCalendar.UpdatedBy = User.Identity?.Name;

                _context.Update(existingCalendar);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Holiday updated successfully!";
                return RedirectToAction(nameof(Index), new { 
                    year = academicCalendar.Date.Year, 
                    month = academicCalendar.Date.Month 
                });
            }

            // Re-populate dropdowns
            var campusesQuery = _context.Campuses.AsQueryable();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusesQuery
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CampusId = new SelectList(campuses, "Id", "Name", academicCalendar.CampusId);
            ViewBag.HolidayTypes = new SelectList(new List<string> { "National", "Religious", "Academic", "Local" }, academicCalendar.HolidayType);

            return View(academicCalendar);
        }

        // POST: AcademicCalendar/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var academicCalendar = await _context.AcademicCalendars.FindAsync(id);
            if (academicCalendar == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            // Check campus access
            if (userCampusId.HasValue && userCampusId.Value > 0 && academicCalendar.CampusId != userCampusId.Value)
                return Forbid();

            // Soft delete
            academicCalendar.IsActive = false;
            academicCalendar.UpdatedAt = DateTime.Now;
            academicCalendar.UpdatedBy = User.Identity?.Name;

            _context.Update(academicCalendar);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Holiday deleted successfully!";
            return RedirectToAction(nameof(Index), new { 
                year = academicCalendar.Date.Year, 
                month = academicCalendar.Date.Month 
            });
        }

        // GET: AcademicCalendar/IsHoliday
        public async Task<IActionResult> IsHoliday(DateTime date, int? campusId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            var targetCampusId = campusId ?? userCampusId ?? 0;

            var isHoliday = await _context.AcademicCalendars
                .AnyAsync(ac => ac.Date.Date == date.Date && 
                               ac.CampusId == targetCampusId &&
                               ac.IsActive);

            return Json(new { isHoliday });
        }
    }
}