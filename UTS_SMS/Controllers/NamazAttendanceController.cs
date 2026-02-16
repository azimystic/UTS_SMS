using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize]
    public class NamazAttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NamazAttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Teacher")]
        // GET: NamazAttendance/Index?date=2024-01-15&personType=&searchString=&campusFilter=
        public async Task<IActionResult> Index(DateTime? date, string personType = "", string searchString = "", int? campusFilter = null)
        {
            var targetDate = date ?? DateTime.Today;
            
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            // Build combined list of students and employees
            var students = _context.Students
                .Where(s => !s.HasLeft)
                .Select(s => new NamazAttendanceViewModel
                {
                    StudentId = s.Id,
                    PersonName = s.StudentName,
                    PersonType = "Student",
                    Status = "WJ",
                    HasAttendanceRecord = false
                });

            var employees = _context.Employees
                .Where(e => e.IsActive)
                .Select(e => new NamazAttendanceViewModel
                {
                    EmployeeId = e.Id,
                    PersonName = e.FullName,
                    PersonType = "Employee",
                    Status = "WJ",
                    HasAttendanceRecord = false
                });

            // Campus filtering
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                students = students.Where(s => _context.Students
                    .Where(st => st.Id == s.StudentId && st.CampusId == userCampusId.Value)
                    .Any());
                
                employees = employees.Where(e => _context.Employees
                    .Where(emp => emp.Id == e.EmployeeId && emp.CampusId == userCampusId.Value)
                    .Any());
            }
            else if (campusFilter.HasValue)
            {
                students = students.Where(s => _context.Students
                    .Where(st => st.Id == s.StudentId && st.CampusId == campusFilter.Value)
                    .Any());
                
                employees = employees.Where(e => _context.Employees
                    .Where(emp => emp.Id == e.EmployeeId && emp.CampusId == campusFilter.Value)
                    .Any());
            }

            // Combine students and employees
            var allPersons = new List<NamazAttendanceViewModel>();
            
            if (string.IsNullOrEmpty(personType) || personType == "Student")
            {
                allPersons.AddRange(await students.ToListAsync());
            }
            
            if (string.IsNullOrEmpty(personType) || personType == "Employee")
            {
                allPersons.AddRange(await employees.ToListAsync());
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                allPersons = allPersons.Where(p => p.PersonName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Get existing namaz attendance records for the target date
            var attendanceRecords = await _context.NamazAttendance
                .Where(na => na.Date == targetDate)
                .ToListAsync();

            // Update status and attendance record flags
            foreach (var person in allPersons)
            {
                var attendanceRecord = attendanceRecords.FirstOrDefault(a => 
                    (person.StudentId.HasValue && a.StudentId == person.StudentId) ||
                    (person.EmployeeId.HasValue && a.EmployeeId == person.EmployeeId));

                if (attendanceRecord != null)
                {
                    person.Status = attendanceRecord.Status;
                    person.HasAttendanceRecord = true;
                    person.Remarks = attendanceRecord.Remarks;
                }
            }

            // Get available campuses for dropdown
            var campusesQuery = _context.Campuses.AsQueryable();

            // If user has a campus, only show that campus
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusesQuery
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Campuses = campuses;
            ViewBag.Date = targetDate;
            ViewBag.PersonType = personType;
            ViewBag.SearchString = searchString;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.Today = DateTime.Today;
            ViewBag.UserCampusId = userCampusId;

            return View(allPersons.OrderBy(p => p.PersonType).ThenBy(p => p.PersonName));
        }

        [Authorize(Roles = "Admin,Teacher")]
        // POST: NamazAttendance/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(List<NamazAttendanceViewModel> model, DateTime date, int? campusFilter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            foreach (var item in model)
            {
                // Determine campus ID
                int targetCampusId;
                if (userCampusId.HasValue && userCampusId.Value > 0)
                {
                    targetCampusId = userCampusId.Value;
                }
                else if (campusFilter.HasValue)
                {
                    targetCampusId = campusFilter.Value;
                }
                else
                {
                    // Get campus from person record
                    if (item.StudentId.HasValue)
                    {
                        var student = await _context.Students.FindAsync(item.StudentId);
                        targetCampusId = student?.CampusId ?? 1;
                    }
                    else if (item.EmployeeId.HasValue)
                    {
                        var employee = await _context.Employees.FindAsync(item.EmployeeId);
                        targetCampusId = employee?.CampusId ?? 1;
                    }
                    else
                    {
                        continue; // Skip if no valid person ID
                    }
                }

                // Find existing record
                var existingRecord = await _context.NamazAttendance
                    .FirstOrDefaultAsync(na => na.Date == date &&
                        ((item.StudentId.HasValue && na.StudentId == item.StudentId) ||
                         (item.EmployeeId.HasValue && na.EmployeeId == item.EmployeeId)));

                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.Status = item.Status;
                    existingRecord.Remarks = item.Remarks;
                    existingRecord.UpdatedAt = DateTime.Now;
                    existingRecord.UpdatedBy = User.Identity?.Name;
                    _context.NamazAttendance.Update(existingRecord);
                }
                else
                {
                    // Get academic year from student if applicable
                    int academicYear = DateTime.Now.Year;
                    if (item.StudentId.HasValue)
                    {
                        var student = await _context.Students.FindAsync(item.StudentId);
                      
                    }

                    // Create new record
                    var namazAttendance = new NamazAttendance
                    {
                        StudentId = item.StudentId,
                        EmployeeId = item.EmployeeId,
                        Date = date,
                        Status = item.Status,
                        Remarks = item.Remarks,
                        CampusId = targetCampusId,
                        AcademicYear = academicYear,
                        CreatedBy = User.Identity?.Name,
                        CreatedAt = DateTime.Now
                    };

                    _context.NamazAttendance.Add(namazAttendance);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Namaz attendance saved successfully!";
            return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd"), campusFilter });
        }

        [Authorize(Roles = "Admin,Teacher")]
        // GET: NamazAttendance/DailySummary?date=2024-01-15
        public async Task<IActionResult> DailySummary(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            // Get namaz attendance records for the target date
            var attendanceQuery = _context.NamazAttendance
                .Include(na => na.Student)
                .Include(na => na.Employee)
                .Where(na => na.Date == targetDate);

            // Apply campus filter
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                attendanceQuery = attendanceQuery.Where(na => na.CampusId == userCampusId.Value);
            }

            var attendanceRecords = await attendanceQuery.ToListAsync();

            // Create summary for students
            var studentSummary = new NamazDailySummaryViewModel
            {
                PersonType = "Students",
                WithJamat = attendanceRecords.Count(a => a.StudentId.HasValue && a.Status == "WJ"),
                Qaza = attendanceRecords.Count(a => a.StudentId.HasValue && a.Status == "QZ"),
                WithoutJamat = attendanceRecords.Count(a => a.StudentId.HasValue && a.Status == "WOJ"),
                TotalPersons = attendanceRecords.Count(a => a.StudentId.HasValue)
            };

            // Create summary for employees
            var employeeSummary = new NamazDailySummaryViewModel
            {
                PersonType = "Employees",
                WithJamat = attendanceRecords.Count(a => a.EmployeeId.HasValue && a.Status == "WJ"),
                Qaza = attendanceRecords.Count(a => a.EmployeeId.HasValue && a.Status == "QZ"),
                WithoutJamat = attendanceRecords.Count(a => a.EmployeeId.HasValue && a.Status == "WOJ"),
                TotalPersons = attendanceRecords.Count(a => a.EmployeeId.HasValue)
            };

            var summaries = new List<NamazDailySummaryViewModel> { studentSummary, employeeSummary };

            ViewBag.Date = targetDate;
            ViewBag.Today = DateTime.Today;

            return View(summaries);
        }
    }
}