// EmployeeAttendanceController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UTS_SMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UTS_SMS.Controllers
{
    [Authorize]
    public class EmployeeAttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;
        private const double ALLOWED_DISTANCE_METERS = 100;

        public EmployeeAttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        [Authorize(Roles = "Admin,Accountant")]
        // GET: EmployeeAttendance/Index?date=2024-01-15
        public async Task<IActionResult> Index(DateTime? date, string roleFilter = "", string searchString = "", int? campusFilter = null)
        {
            var targetDate = date ?? DateTime.Today;
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            // Get all active employees with filtering
            var employeesQuery = _context.Employees
                .Where(e => e.IsActive)
                .AsNoTracking();

            // Apply campus filter
            if (campusFilter.HasValue)
            {
                employeesQuery = employeesQuery.Where(e => e.CampusId == campusFilter.Value);
            }
            else if (campusId.HasValue)
            {
                employeesQuery = employeesQuery.Where(e => e.CampusId == campusId.Value);
                campusFilter = campusId.Value; // Set the filter to user's campus
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                employeesQuery = employeesQuery.Where(e => e.Role == roleFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                employeesQuery = employeesQuery.Where(e =>
                    e.FullName.Contains(searchString) ||
                    e.CNIC.Contains(searchString) ||
                    e.PhoneNumber.Contains(searchString));
            }

            var employees = await employeesQuery
                .OrderBy(e => e.Role)
                .ThenBy(e => e.FullName)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Role
                })
                .ToListAsync();

            // Get attendance records for the target date
            var employeeIds = employees.Select(e => e.Id).ToList();
            var attendanceRecords = await _context.EmployeeAttendance
                .Where(a => a.Date == targetDate && employeeIds.Contains(a.EmployeeId))
                .AsNoTracking()
                .Select(a => new
                {
                    a.EmployeeId,
                    a.Status,
                    a.TimeIn,
                    a.TimeOut
                })
                .ToListAsync();

            // Create view model
            var viewModel = employees.Select(employee =>
            {
                var attendance = attendanceRecords.FirstOrDefault(a => a.EmployeeId == employee.Id);
                return new EmployeeAttendanceViewModel
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    Role = employee.Role,
                    Status = attendance?.Status ?? "A",
                    HasAttendanceRecord = attendance != null,
                    TimeIn = attendance?.TimeIn,
                    TimeOut = attendance?.TimeOut
                };
            }).ToList();

            // Cache campuses and roles to reduce database queries
            var cacheKey = $"Campuses_{campusId}";
            var campuses = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                
                var query = _context.Campuses.AsNoTracking();
                if (campusId.HasValue)
                {
                    query = query.Where(c => c.Id == campusId.Value);
                }
                
                return await query.OrderBy(c => c.Name).ToListAsync();
            });

            var rolesCacheKey = "EmployeeRoles";
            var roles = await _cache.GetOrCreateAsync(rolesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                
                return await _context.Employees
                    .Where(e => e.IsActive)
                    .Select(e => e.Role)
                    .Distinct()
                    .OrderBy(r => r)
                    .AsNoTracking()
                    .ToListAsync();
            });

            ViewBag.Roles = roles;
            ViewBag.Campuses = campuses;
            ViewBag.Date = targetDate;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.SearchString = searchString;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.Today = DateTime.Today;
            ViewBag.UserCampusId = campusId;

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Accountant")]
        // POST: EmployeeAttendance/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(List<EmployeeAttendanceViewModel> model, DateTime date)
        {
            // Remove unnecessary ModelState keys for all items
            ModelState.Remove("date");
            for (int i = 0; i < model.Count; i++)
            {
                ModelState.Remove($"[{i}].Role");
                ModelState.Remove($"[{i}].EmployeeName");
                ModelState.Remove($"[{i}].HasAttendanceRecord");
                ModelState.Remove($"[{i}].TimeIn");
                ModelState.Remove($"[{i}].TimeOut");
            }
            
            if (ModelState.IsValid)
            {
                var employeeIds = model.Select(m => m.EmployeeId).ToList();
                
                // Get existing records and employee times in single queries
                var existingRecords = await _context.EmployeeAttendance
                    .Where(a => a.Date == date && employeeIds.Contains(a.EmployeeId))
                    .ToDictionaryAsync(a => a.EmployeeId);

                var employeeTimes = await _context.Employees
                    .Where(e => employeeIds.Contains(e.Id))
                    .AsNoTracking()
                    .Select(e => new
                    {
                        e.Id,
                        e.OnTime,
                        e.OffTime,
                        e.LateTimeFlexibility
                    })
                    .ToDictionaryAsync(e => e.Id);

                foreach (var item in model)
                {
                    var times = employeeTimes.GetValueOrDefault(item.EmployeeId);
                    
                    if (existingRecords.TryGetValue(item.EmployeeId, out var existingRecord))
                    {
                        if (existingRecord.Status != item.Status)
                        {
                            UpdateAttendanceStatus(existingRecord, item.Status, times, date);
                            existingRecord.UpdatedAt = DateTime.Now;
                            existingRecord.UpdatedBy = User.Identity.Name;
                            _context.EmployeeAttendance.Update(existingRecord);
                        }
                    }
                    else
                    {
                        var attendance = CreateAttendance(item, date, times, User.Identity.Name);
                        _context.EmployeeAttendance.Add(attendance);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Employee attendance for {date:MMMM dd, yyyy} saved successfully!";
                return RedirectToAction("DailySummary", new { date = date.ToString("yyyy-MM-dd") });
            }

            // Repopulate ViewBag on validation failure
            await PopulateViewBagAsync();
            ViewBag.Date = date;
            
            return View("Index", model);
        }

        private void UpdateAttendanceStatus(EmployeeAttendance record, string newStatus, dynamic times, DateTime date)
        {
            switch (newStatus)
            {
                case "S": // Short leave
                    record.TimeOut = DateTime.Now;
                    break;
                case "A": case "L": // Absent / Leave
                    record.TimeOut = null;
                    record.TimeIn = null;
                    break;
                case "P": // Present
                    if (times?.OffTime != null)
                        record.TimeOut = date.Date.Add(times.OffTime.ToTimeSpan());
                    if (record.TimeIn == null && times?.OnTime != null)
                        record.TimeIn = date.Date.Add(times.OnTime.ToTimeSpan());
                    break;
                default:
                    if (times?.OffTime != null)
                        record.TimeOut = date.Date.Add(times.OffTime.ToTimeSpan());
                    if (record.TimeIn == null)
                        record.TimeIn = DateTime.Now;
                    break;
            }
            record.Status = newStatus;
        }

        private EmployeeAttendance CreateAttendance(EmployeeAttendanceViewModel item, DateTime date, dynamic times, string username)
        {
            var attendance = new EmployeeAttendance
            {
                EmployeeId = item.EmployeeId,
                Date = date,
                Status = item.Status,
                TimeIn = item.Status == "P" ? DateTime.Now : null,
                TimeOut = (item.Status == "P" && times?.OffTime != null) 
                    ? date.Date.Add(times.OffTime.ToTimeSpan()) 
                    : null,
                CreatedBy = username,
                CreatedAt = DateTime.Now
            };

            if (item.Status == "P" && times?.OnTime != null)
            {
                var todayOnTime = date.Date.Add(times.OnTime.ToTimeSpan());
                var allowedTime = todayOnTime.AddMinutes(times.LateTimeFlexibility + 5);

                if (attendance.TimeIn.HasValue && attendance.TimeIn.Value > allowedTime)
                {
                    attendance.Status = "T";
                }
            }

            return attendance;
        }

        private async Task PopulateViewBagAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var campusesQuery = _context.Campuses.AsNoTracking();
            if (campusId.HasValue)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == campusId.Value);
            }

            ViewBag.Campuses = await campusesQuery.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Roles = await _context.Employees
                .Where(e => e.IsActive)
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.Today = DateTime.Today;
            ViewBag.UserCampusId = campusId;
            ViewBag.RoleFilter = "";
            ViewBag.SearchString = "";
            ViewBag.CampusFilter = campusId;
        }

        [Authorize]
        // GET: EmployeeAttendance/MyAttendance?year=2024&month=1
        public async Task<IActionResult> MyAttendance(int? year, int? month)
        {
            var currentYear = year ?? DateTime.Today.Year;
            var currentMonth = month ?? DateTime.Today.Month;

            var firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var currentUser = await _userManager.GetUserAsync(User);
            var teacher = await _context.Employees
                .Where(e => e.Id == currentUser.EmployeeId)
                .Select(e => new { e.Id, e.FullName })
                .AsNoTracking()
                .FirstOrDefaultAsync();
                
            var employeeId = teacher?.Id ?? 0;
            if (employeeId == 0) return RedirectToAction("Login", "Account");

            var attendanceHistory = await _context.EmployeeAttendance
                .Where(a => a.EmployeeId == employeeId && 
                           a.Date >= firstDayOfMonth && 
                           a.Date <= lastDayOfMonth)
                .OrderBy(a => a.Date)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.EmployeeName = teacher?.FullName;
            ViewBag.CurrentYear = currentYear;
            ViewBag.CurrentMonth = currentMonth;
            ViewBag.MonthName = firstDayOfMonth.ToString("MMMM");

            return View(attendanceHistory);
        }

        [Authorize(Roles = "Admin,Accountant")]
        // GET: EmployeeAttendance/DailySummary?date=2024-01-15
        public async Task<IActionResult> DailySummary(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;

            // Get summary by role
            var dailySummary = await _context.EmployeeAttendance
                .Where(a => a.Date == targetDate)
                .Join(_context.Employees.Where(e => e.IsActive),
                    a => a.EmployeeId,
                    e => e.Id,
                    (a, e) => new { a.Status, e.Role })
                .GroupBy(x => x.Role)
                .Select(g => new EmployeeDailySummaryViewModel
                {
                    Role = g.Key,
                    Present = g.Count(x => x.Status == "P"),
                    Absent = g.Count(x => x.Status == "A"),
                    Leave = g.Count(x => x.Status == "L"),
                    Late = g.Count(x => x.Status == "T"),
                    ShortLeave = g.Count(x => x.Status == "S"),
                    TotalEmployees = _context.Employees.Count(e => e.Role == g.Key && e.IsActive)
                })
                .OrderBy(d => d.Role)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Date = targetDate;

            return View(dailySummary);
        }

        [Authorize]
        // GET: EmployeeAttendance/MarkMyAttendance
        public IActionResult MarkMyAttendance()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMyAttendance(string status, double? latitude, double? longitude)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var teacher = await _context.Employees
                .Where(e => e.Id == currentUser.EmployeeId)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
                
            var employeeId = teacher;
            if (employeeId == 0) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;
            var now = DateTime.Now;

            var existingRecord = await _context.EmployeeAttendance
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            bool isLocationValid = false;
            if (latitude.HasValue && longitude.HasValue)
            {
                const double WORK_LATITUDE = 24.8607;
                const double WORK_LONGITUDE = 67.0011;
                isLocationValid = CalculateDistance(latitude.Value, longitude.Value, WORK_LATITUDE, WORK_LONGITUDE) <= ALLOWED_DISTANCE_METERS;
            }
            
            if (isLocationValid)
            {
                if (existingRecord == null)
                {
                    var attendance = new EmployeeAttendance
                    {
                        EmployeeId = employeeId,
                        Date = today,
                        TimeIn = now,
                        Status = status,
                        Latitude = latitude,
                        Longitude = longitude,
                        IsLocationValid = isLocationValid,
                        CreatedBy = User.Identity.Name,
                        CreatedAt = now
                    };

                    _context.EmployeeAttendance.Add(attendance);
                    TempData["SuccessMessage"] = status == "S" ? "Checked in with Short Leave." : "Check-in successful!";
                }
                else
                {
                    if (existingRecord.TimeOut == null)
                    {
                        existingRecord.TimeOut = now;
                        existingRecord.UpdatedAt = now;
                        existingRecord.UpdatedBy = User.Identity.Name;
                        _context.EmployeeAttendance.Update(existingRecord);

                        TempData["SuccessMessage"] = existingRecord.Status == "S"
                            ? "Checked out (Short Leave)."
                            : "Check-out successful!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "You have already checked out today.";
                        return RedirectToAction("MarkMyAttendance");
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("MarkMyAttendance");
            }

            TempData["ErrorMessage"] = "Kindly Enter Premises of School";
            return RedirectToAction("MarkMyAttendance");
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371000;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}