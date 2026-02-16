using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    public class ParentDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParentDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ParentDashboard
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.FamilyId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var family = await _context.Families
                .Include(f => f.Students)
                    .ThenInclude(s => s.ClassObj)
                .Include(f => f.Students)
                    .ThenInclude(s => s.SectionObj)
                .FirstOrDefaultAsync(f => f.Id == currentUser.FamilyId);

            if (family == null)
            {
                return NotFound();
            }

            var viewModel = new ParentDashboardViewModel
            {
                Family = family,
                Students = family.Students.Where(s => !s.HasLeft).ToList()
            };

            return View(viewModel);
        }

        // GET: ParentDashboard/StudentDetails/5
        public async Task<IActionResult> StudentDetails(int id, int? month, int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.FamilyId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Family)
                .Include(s => s.SubjectsGrouping)
                .FirstOrDefaultAsync(s => s.Id == id && s.FamilyId == currentUser.FamilyId);

            if (student == null)
            {
                return NotFound();
            }

            // Default to current month if not specified
            var currentDate = DateTime.Now;
            var selectedMonth = month ?? currentDate.Month;
            var selectedYear = year ?? currentDate.Year;
            var startDate = new DateTime(selectedYear, selectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get attendance data
            var attendanceData = await _context.Attendance
                .Where(a => a.StudentId == id && a.Date >= startDate && a.Date <= endDate)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Get namaz attendance
            var namazAttendance = await _context.NamazAttendance
                .Where(na => na.StudentId == id && na.Date >= startDate && na.Date <= endDate)
                .ToListAsync();

            // Get test results for the academic year
            var testResults = await _context.ExamMarks
                .Include(em => em.Exam)
                .Where(em => em.StudentId == id)
                .OrderBy(em => em.CreatedDate)
                .ToListAsync();

            // Get fee records
            var feeRecord = await _context.BillingMaster
                .Include(bm => bm.Transactions)
                .Where(bm => bm.StudentId == id)
                .OrderByDescending(bm => bm.CreatedDate)
                .FirstOrDefaultAsync();

            // Get student position/ranking if available - just use latest exam marks
            var latestExamResult = await _context.ExamMarks
                .Where(em => em.StudentId == id)
                .OrderByDescending(em => em.Id)
                .Select(em => new { em.TotalMarks, em.ObtainedMarks, em.Percentage, em.Grade })
                .FirstOrDefaultAsync();

            // Get diary entries for today or selected date
            var todayDiary = await _context.Diaries
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(d => d.DiaryImages)
                .Where(d => d.Date.Date == DateTime.Today.Date && 
                           d.TeacherAssignment.ClassId == student.Class &&
                           d.TeacherAssignment.SectionId == student.Section)
                .ToListAsync();

            // Get active complaints (only those registered by admin or teacher, not by student)
            var activeComplaints = await _context.StudentComplaints
                .Where(sc => sc.StudentId == id && 
                            sc.Status != "Resolved" && 
                            sc.ReporterType != "Student")
                .OrderByDescending(sc => sc.CreatedDate)
                .ToListAsync();

            // Get student duties (current and upcoming)
            var studentDuties = await _context.AssignedDutyStudents
                .Include(ads => ads.AssignedDuty)
                .Where(ads => ads.StudentId == id && 
                             ads.IsActive && 
                             (ads.AssignedDuty.Status == "Assigned" || ads.AssignedDuty.Status == "In Progress") &&
                             ads.AssignedDuty.IsActive)
                .OrderBy(ads => ads.AssignedDuty.StartDate)
                .ToListAsync();

            var viewModel = new ParentStudentDetailsViewModel
            {
                Student = student,
                AttendanceData = attendanceData.Cast<dynamic>().ToList(),
                NamazAttendance = namazAttendance,
                TestResults = testResults,
                FeeRecord = feeRecord,
                LatestExamResult = latestExamResult,
                TodayDiary = todayDiary,
                ActiveComplaints = activeComplaints,
                StudentDuties = studentDuties,
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: ParentDashboard/DiaryDetails
        public async Task<IActionResult> DiaryDetails(int studentId, DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.FamilyId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == studentId && s.FamilyId == currentUser.FamilyId);

            if (student == null)
            {
                return NotFound();
            }

            var selectedDate = date ?? DateTime.Today;

            var diaryEntries = await _context.Diaries
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(d => d.DiaryImages)
                .Where(d => d.Date.Date == selectedDate.Date &&
                           d.TeacherAssignment.ClassId == student.Class &&
                           d.TeacherAssignment.SectionId == student.Section)
                .OrderBy(d => d.TeacherAssignment.Subject.Name)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.SelectedDate = selectedDate;

            return View(diaryEntries);
        }

        // GET: ParentDashboard/DownloadDiaryImage/5
        public async Task<IActionResult> DownloadDiaryImage(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.FamilyId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var diaryImage = await _context.DiaryImages
                .Include(di => di.Diary)
                    .ThenInclude(d => d.TeacherAssignment)
                .FirstOrDefaultAsync(di => di.Id == id);

            if (diaryImage == null)
            {
                return NotFound();
            }

            // Verify the student belongs to the parent's family
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Class == diaryImage.Diary.TeacherAssignment.ClassId && 
                                         s.Section == diaryImage.Diary.TeacherAssignment.SectionId && 
                                         s.FamilyId == currentUser.FamilyId);

            if (student == null)
            {
                return NotFound();
            }

            // Check if file exists
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", diaryImage.ImagePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            // Get file content
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = diaryImage.OriginalFileName ?? Path.GetFileName(diaryImage.ImagePath);
            var mimeType = GetMimeType(fileName);

            return File(fileBytes, mimeType, fileName);
        }

        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        // GET: ParentDashboard/StudentTimetable/5
        public async Task<IActionResult> StudentTimetable(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.FamilyId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Family)
                .FirstOrDefaultAsync(s => s.Id == id && s.FamilyId == currentUser.FamilyId);

            if (student == null)
            {
                return NotFound();
            }

            // Get the timetable for this student's class and section
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                        .ThenInclude(ta => ta.Subject)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                        .ThenInclude(ta => ta.Teacher)
                .FirstOrDefaultAsync(t => t.ClassId == student.Class && 
                                        t.SectionId == student.Section && 
                                        t.IsActive);

            ViewBag.Student = student;
            return View(timetable);
        }

        // GET: ParentDashboard/AcademicCalendar
        public async Task<IActionResult> AcademicCalendar(int? studentId, int? year, int? month)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.FamilyId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Get student if specified, otherwise get first student
            Student student = null;
            if (studentId.HasValue)
            {
                student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .FirstOrDefaultAsync(s => s.Id == studentId.Value && s.FamilyId == currentUser.FamilyId);
            }

            if (student == null)
            {
                student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .FirstOrDefaultAsync(s => s.FamilyId == currentUser.FamilyId);
            }

            if (student == null)
            {
                return NotFound();
            }

            var currentDate = DateTime.Now;
            var selectedYear = year ?? currentDate.Year;
            var selectedMonth = month ?? currentDate.Month;

            // Get academic calendar for the student's campus
            var holidays = await _context.AcademicCalendars
                .Where(ac => ac.CampusId == student.CampusId &&
                            ac.Date.Year == selectedYear &&
                            ac.Date.Month == selectedMonth &&
                            ac.IsActive)
                .OrderBy(ac => ac.Date)
                .ToListAsync();

            // Get all students in family for selector
            var familyStudents = await _context.Students
                .Where(s => s.FamilyId == currentUser.FamilyId && !s.HasLeft)
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.FamilyStudents = familyStudents;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.MonthName = new DateTime(selectedYear, selectedMonth, 1).ToString("MMMM yyyy");

            return View(holidays);
        }

        public class ParentDashboardViewModel
        {
            public Family Family { get; set; }
            public List<Student> Students { get; set; } = new();
        }

        public class ParentStudentDetailsViewModel
        {
            public Student Student { get; set; }
            public List<dynamic> AttendanceData { get; set; } = new();
            public List<NamazAttendance> NamazAttendance { get; set; } = new();
            public List<ExamMarks> TestResults { get; set; } = new();
            public BillingMaster? FeeRecord { get; set; }
            public dynamic? LatestExamResult { get; set; }
            public List<Diary> TodayDiary { get; set; } = new();
            public List<StudentComplaint> ActiveComplaints { get; set; } = new();
            public List<AssignedDutyStudent> StudentDuties { get; set; } = new();
            public int SelectedMonth { get; set; }
            public int SelectedYear { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
    }
}