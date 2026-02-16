using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherPerformanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherPerformanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TeacherPerformance - Teacher of the Month ranking
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            // Default to current month/year if not specified
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            // Get teacher performances for the specified month/year
            var performancesQuery = _context.TeacherPerformances
                .Include(tp => tp.Teacher)
                .Include(tp => tp.Campus)
                .Where(tp => tp.Month == currentMonth && tp.Year == currentYear && tp.IsActive);

            // Apply campus filter
            if (campusId.HasValue && campusId > 0)
            {
                performancesQuery = performancesQuery.Where(tp => tp.CampusId == campusId);
            }

            var performances = await performancesQuery
                .OrderByDescending(tp => tp.TotalScore)
                .ToListAsync();

            // Get top 3 performers
            var topPerformers = performances.Take(3).ToList();

            ViewBag.CurrentMonth = currentMonth;
            ViewBag.CurrentYear = currentYear;
            ViewBag.TopPerformers = topPerformers;
            ViewBag.AllPerformances = performances;

            // Campus filter for dropdown
            ViewBag.Campuses = campusId.HasValue && campusId > 0 
                ? await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync()
                : await _context.Campuses.Where(c => c.IsActive).ToListAsync();

            return View(performances);
        }

        // GET: TeacherPerformance/Calculate - Calculate performance for current month
        // GET: TeacherPerformance/Calculate - Calculate performance for current month
        public async Task<IActionResult> Calculate(int? month, int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            // Default to current month/year if not specified
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;

            // Get all active teachers
            var teachersQuery = _context.Employees
                .Where(e => e.IsActive && e.Role.Contains("Teacher"));

            if (campusId.HasValue && campusId > 0)
            {
                teachersQuery = teachersQuery.Where(e => e.CampusId == campusId);
            }

            var teachers = await teachersQuery.ToListAsync();

            var calculatedPerformances = new List<TeacherPerformance>();

            foreach (var teacher in teachers)
            {
                var performance = await CalculateTeacherPerformance(teacher.Id, targetMonth, targetYear);
                if (performance != null)
                {
                    calculatedPerformances.Add(performance);
                }
            }

            // Keep view bag keys consistent with what the view expects
            ViewBag.TargetMonth = targetMonth;
            ViewBag.TargetYear = targetYear;
            ViewBag.CalculatedPerformances = calculatedPerformances;
            ViewBag.TopPerformers = calculatedPerformances.OrderByDescending(p => p.TotalScore).ToList();
            ViewBag.TeachersCount = teachers.Count;

            return View(calculatedPerformances);
        }

        // POST: TeacherPerformance/SaveCalculations - Save calculated performances
        [HttpPost]
        public async Task<IActionResult> SaveCalculations(int month, int year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            // Remove existing performances for the month/year
            var existingPerformances = await _context.TeacherPerformances
                .Where(tp => tp.Month == month && tp.Year == year)
                .ToListAsync();

            if (campusId.HasValue && campusId > 0)
            {
                existingPerformances = existingPerformances.Where(tp => tp.CampusId == campusId).ToList();
            }

            _context.TeacherPerformances.RemoveRange(existingPerformances);

            // Calculate and save new performances
            var teachersQuery = _context.Employees
                .Where(e => e.IsActive && e.Role.Contains("Teacher"));

            if (campusId.HasValue && campusId > 0)
            {
                teachersQuery = teachersQuery.Where(e => e.CampusId == campusId);
            }

            var teachers = await teachersQuery.ToListAsync();

            foreach (var teacher in teachers)
            {
                var performance = await CalculateTeacherPerformance(teacher.Id, month, year);
                if (performance != null)
                {
                    performance.CreatedBy = User.Identity?.Name ?? "System";
                    performance.CreatedDate = DateTime.Now;
                    _context.TeacherPerformances.Add(performance);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Teacher performances calculated and saved for {GetMonthName(month)} {year}";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        private async Task<TeacherPerformance?> CalculateTeacherPerformance(int teacherId, int month, int year)
        {
            var teacher = await _context.Employees.Include(e => e.Campus).FirstOrDefaultAsync(e => e.Id == teacherId);
            if (teacher == null) return null;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var performance = new TeacherPerformance
            {
                TeacherId = teacherId,
                Month = month,
                Year = year,
                CampusId = teacher.CampusId
            };

            // Get holidays from calendar events for this campus
            var holidays = await _context.CalendarEvents
                .Where(ce => ce.CampusId == teacher.CampusId && 
                            ce.IsHoliday && 
                            ce.IsActive &&
                            ce.StartDate <= endDate &&
                            (ce.EndDate == null || ce.EndDate >= startDate))
                .ToListAsync();

            // Calculate actual working days (excluding Sundays and holidays)
            var workingDays = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Skip Sundays
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Skip holidays
                var isHoliday = holidays.Any(h => 
                    date >= h.StartDate.Date && 
                    date <= (h.EndDate?.Date ?? h.StartDate.Date));
                
                if (!isHoliday)
                    workingDays.Add(date);
            }

            // 1. Monthly Attendance Score (3.5 marks max)
            var attendanceRecords = await _context.EmployeeAttendance
                .Where(ea => ea.EmployeeId == teacherId && ea.Date >= startDate && ea.Date <= endDate)
                .ToListAsync();

            var totalWorkingDays = workingDays.Count;
            var attendedDays = attendanceRecords.Count(ea => ea.Status == "P");
            
            performance.TotalWorkingDays = totalWorkingDays;
            performance.AttendedDays = attendedDays;
            performance.AttendanceScore = totalWorkingDays > 0 ? (decimal)(attendedDays * 3.5 / totalWorkingDays) : 0;

            // 2. Punctuality Score (2.5 marks max) - treating late as absent, only counting on-time present days
            var onTimeRecords = attendanceRecords.Where(ea => ea.Status == "P" && 
                ea.TimeIn.HasValue && teacher.Campus?.StartTime != null &&
                TimeOnly.FromDateTime(ea.TimeIn.Value) <= teacher.Campus.StartTime.Add(TimeSpan.FromMinutes(15))).Count();
            
            performance.OnTimeDays = onTimeRecords;
            // Calculate punctuality based on total working days (not attended days), treating late as absent
            performance.PunctualityScore = totalWorkingDays > 0 ? (decimal)(onTimeRecords * 2.5 / totalWorkingDays) : 0;

            // 3. Test Average Score (5.5 marks max)
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .ToListAsync();

            var testMarks = new List<decimal>();
            foreach (var assignment in teacherAssignments)
            {
                var examMarks = await _context.ExamMarks
                    .Where(em => em.SubjectId == assignment.SubjectId && 
                               em.ClassId == assignment.ClassId && 
                               em.SectionId == assignment.SectionId &&
                               em.CreatedDate >= startDate && em.CreatedDate <= endDate)
                    .ToListAsync();

                if (examMarks.Any())
                {
                    var avgPercentage = examMarks.Average(em => em.Percentage);
                    testMarks.Add(avgPercentage);
                }
            }

            // If teacher has test marks, calculate average; otherwise give full marks (benefit of doubt)
            performance.AverageTestMarks = testMarks.Any() ? testMarks.Average() : 100;
            performance.TestAverageScore = performance.AverageTestMarks * 5.5m / 100m;

            // 4. Survey Score (6 marks max)
            var surveyResponses = await (
    from sr in _context.StudentSurveyResponses.Include(s => s.Student)
    join ta in _context.TeacherAssignments
        on new { ClassId = sr.Student.Class, SectionId = sr.Student.Section }
        equals new { ta.ClassId, ta.SectionId }
    where ta.TeacherId == teacherId
          && ta.IsActive
          && sr.ResponseDate >= startDate
          && sr.ResponseDate <= endDate
    select sr
).ToListAsync();


            performance.TotalSurveyResponses = surveyResponses.Count;
            performance.PositiveSurveyResponses = surveyResponses.Count(sr => sr.Response == true);
            // If no survey responses, give full marks (benefit of doubt)
            performance.SurveyScore = performance.TotalSurveyResponses > 0 ? 
                (decimal)(performance.PositiveSurveyResponses * 6.0 / performance.TotalSurveyResponses) : 6.0m;

            // 5. Test Return Score (1.5 marks max)
            var testReturns = await _context.TestReturns
                .Where(tr => tr.TeacherId == teacherId && 
                           tr.ExamDate >= startDate && tr.ExamDate <= endDate)
                .ToListAsync();

            performance.TotalTestsToReturn = testReturns.Count;
            performance.TestsReturnedOnTime = testReturns.Count(tr => tr.IsReturnedOnTime);
            
            // If teacher has no tests to return, give full marks; otherwise calculate based on returns
            performance.TestReturnScore = performance.TotalTestsToReturn > 0 ? 
                (decimal)(performance.TestsReturnedOnTime * 1.5 / performance.TotalTestsToReturn) : 1.5m;

            // 6. Checking Quality Score (1 mark max)
            performance.GoodCheckingCount = testReturns.Count(tr => tr.CheckingQuality == "Good");
            performance.BetterCheckingCount = testReturns.Count(tr => tr.CheckingQuality == "Better");
            performance.BadCheckingCount = testReturns.Count(tr => tr.CheckingQuality == "Bad");

            var qualityScore = 0m;
            if (performance.TotalTestsToReturn > 0)
            {
                qualityScore = (performance.BetterCheckingCount * 1m + performance.GoodCheckingCount * 0.7m) / performance.TotalTestsToReturn;
            }
            else
            {
                // If no tests to return, give full marks for quality
                qualityScore = 1m;
            }
            performance.CheckingQualityScore = Math.Min(qualityScore, 1m);

            // Calculate total score
            performance.CalculateTotalScore();

            return performance;
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January", 2 => "February", 3 => "March", 4 => "April",
                5 => "May", 6 => "June", 7 => "July", 8 => "August",
                9 => "September", 10 => "October", 11 => "November", 12 => "December",
                _ => "Unknown"
            };
        }

        // GET: TeacherPerformance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var performance = await _context.TeacherPerformances
                .Include(tp => tp.Teacher)
                .Include(tp => tp.Campus)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (performance == null)
                return NotFound();

            return View(performance);
        }
    }
}