using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UTS_SMS.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }
        [HttpGet]
        public async Task<JsonResult> GetClassesByCampus(int campusId)
        {
            var campuses = await _context.Classes
                .Where(s => s.CampusId == campusId)
                .Select(s => new { id = s.Id, name = s.Name })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(campuses);
        }
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(s => s.ClassId == classId)
                .Select(s => new { id = s.Id, name = s.Name })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(sections);
        }
        [Authorize(Roles = "Admin,Teacher")]
        // GET: Attendance/Index?classId=1&sectionId=2&date=2024-01-15
        public async Task<IActionResult> Index(int? campusId, int? classId, int? sectionId, DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var usercampusId = campusId;
            if (campusId == null)
            {
                  usercampusId = currentUser?.CampusId;
            }
 
            // Get available classes
            IQueryable<Student> studentQuery = _context.Students.Where(s => !s.HasLeft);

            if (usercampusId != null)
            {
                studentQuery = studentQuery.Where(s => s.CampusId == usercampusId);
            }

            // Filter classes for teachers based on their assignments
            List<dynamic> classes;
            List<dynamic> sections = new List<dynamic>();
            
            if (User.IsInRole("Teacher") && currentUser.EmployeeId.HasValue)
            {
                // Get classes the teacher is assigned to
                var teacherClassIds = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == currentUser.EmployeeId.Value && ta.IsActive)
                    .Select(ta => ta.ClassId)
                    .Distinct()
                    .ToListAsync();

                classes = await studentQuery
                    .Where(s => teacherClassIds.Contains(s.ClassObj.Id))
                    .Select(s => new { s.ClassObj.Id, s.ClassObj.Name })
                    .Distinct()
                    .OrderBy(c => c.Name)
                    .ToListAsync<dynamic>();
                
                // If class selected, filter sections by teacher assignments
                if (classId.HasValue)
                {
                    var teacherSectionIds = await _context.TeacherAssignments
                        .Where(ta => ta.TeacherId == currentUser.EmployeeId.Value && 
                                    ta.ClassId == classId.Value && 
                                    ta.IsActive)
                        .Select(ta => ta.SectionId)
                        .Distinct()
                        .ToListAsync();

                    sections = await _context.ClassSections
                        .Where(s => s.ClassId == classId.Value && teacherSectionIds.Contains(s.Id))
                        .Select(s => new { s.Id, s.Name })
                        .Distinct()
                        .OrderBy(s => s.Name)
                        .ToListAsync<dynamic>();
                }
            }
            else
            {
                // Admin sees all classes
                classes = await studentQuery
                    .Select(s => new { s.ClassObj.Id, s.ClassObj.Name })
                    .Distinct()
                    .OrderBy(c => c.Name)
                    .ToListAsync<dynamic>();

                // Get available sections for the selected class
                if (classId.HasValue)
                {
                    sections = await _context.ClassSections
                        .Where(s => s.ClassId == classId.Value)
                        .Select(s => new { s.Id, s.Name })
                        .Distinct()
                        .OrderBy(s => s.Name)
                        .ToListAsync<dynamic>();
                }
            }

            var campuses = await studentQuery
                .Select(s => new { s.Campus.Id, s.Campus.Name })
                .Distinct()
                .OrderBy(c => c.Name)
                .ToListAsync();

            // FIX: Pass classes with correct property names
            ViewBag.Classes = classes;
            ViewBag.Campuses = campuses;
            ViewBag.Sections = sections;

            // If no class/section selected, show selection UI
            if (!classId.HasValue || !sectionId.HasValue)
            {
                ViewBag.CampusId = usercampusId;
                ViewBag.ClassId = classId;
                return View(new List<AttendanceViewModel>());
            }

            var targetDate = date ?? DateTime.Today;

            // Get class and section names for display
            var selectedClass = await _context.Classes.FindAsync(classId.Value);
            var selectedSection = await _context.ClassSections.FindAsync(sectionId.Value);

            // Get all students for the class and section
            var studentFilterQuery = _context.Students
                .Where(s => s.Class == classId.Value && s.Section == sectionId.Value && !s.HasLeft);

            if (usercampusId != null)
            {
                studentFilterQuery = studentFilterQuery.Where(s => s.CampusId == usercampusId);
            }

            var students = await studentFilterQuery
                .OrderBy(s => s.StudentName)
                .ToListAsync();

            // Get attendance records for the target date
            var studentIds = students.Select(s => s.Id).ToList();
            var attendanceRecords = await _context.Attendance
                .Where(a => a.Date == targetDate && studentIds.Contains(a.StudentId))
                .ToListAsync();

            // Create view model
            var viewModel = students.Select(student => new AttendanceViewModel
            {
                StudentId = student.Id,
                StudentName = student.StudentName ?? "Unknown", // Handle null names
                Status = attendanceRecords.FirstOrDefault(a => a.StudentId == student.Id)?.Status ?? "A",
                HasAttendanceRecord = attendanceRecords.Any(a => a.StudentId == student.Id)
            }).ToList();
            ViewBag.CampusId = usercampusId;
            ViewBag.ClassId = classId;
            ViewBag.SectionId = sectionId;
            ViewBag.Class = selectedClass?.Name ?? "Unknown Class";
            ViewBag.Section = selectedSection?.Name ?? "Unknown Section";
            ViewBag.Date = targetDate;
            ViewBag.Today = DateTime.Today;

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Teacher")]
        // POST: Attendance/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(List<AttendanceViewModel> model, int campusId, int classId, int sectionId, DateTime date)
        {
            foreach (var key in ModelState.Keys.Where(k => k.Contains("StudentName")).ToList())
            {
                ModelState.Remove(key);
            }
            var currentUser = await _userManager.GetUserAsync(User);
            var usercampusId = campusId;
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                var existingRecords = await _context.Attendance
                    .Where(a => a.Date == date && a.ClassId == classId && a.SectionId == sectionId && a.CampusId == usercampusId)
                    .ToListAsync();

                // Get all students to retrieve their academic years
                var studentIds = model.Select(m => m.StudentId).ToList();
                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

                foreach (var item in model)
                {
                    var existingRecord = existingRecords.FirstOrDefault(a => a.StudentId == item.StudentId);

                    if (existingRecord != null)
                    {
                        existingRecord.CampusId = (int)usercampusId;
                        // Update existing record
                        existingRecord.Status = item.Status;
                        existingRecord.UpdatedAt = DateTime.Now;
                        existingRecord.UpdatedBy = "Azeem";
                        _context.Attendance.Update(existingRecord);
                    }
                    else
                    {
                        // Create new record
                        var attendance = new Attendance
                        {
                            StudentId = item.StudentId,
                            Date = date,
                            Status = item.Status,
                            ClassId = classId,
                            SectionId = sectionId,
                            CreatedBy = "Azeem",
                            CreatedAt = DateTime.Now,
                            CampusId = (int)usercampusId,
                        };
                        _context.Attendance.Add(attendance);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Attendance for {date.ToString("MMMM dd, yyyy")} saved successfully!";
                return RedirectToAction("Summary", new { classId, sectionId, date = date.ToString("yyyy-MM-dd") });
            }

            // Re-populate dropdown data (this was missing)
            ViewBag.Classes = await _context.Students
                .Where(s => !s.HasLeft && s.CampusId == usercampusId)
                .Select(s => new { s.ClassObj.Id, s.ClassObj.Name })
                .Distinct()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Sections = await _context.ClassSections
                .Where(s => s.ClassId == classId)
                .Select(s => new { s.Id, s.Name })
                .Distinct()
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.CampusId = usercampusId;
            ViewBag.ClassId = classId;
            ViewBag.SectionId = sectionId;
            ViewBag.Date = date;

            return View("Index", model);
        }

        [Authorize(Roles = "Admin,Teacher,Student")]
        // GET: Attendance/History?studentId=1
        public async Task<IActionResult> History(int studentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return NotFound();
            }

            var attendanceHistory = await _context.Attendance
                .Where(a => a.StudentId == studentId && a.CampusId == campusId)
                .OrderByDescending(a => a.Date)
                .Take(365) // Last 365 days for yearly analysis
                .ToListAsync();

            ViewBag.StudentName = student.StudentName;
            ViewBag.Class = student.ClassObj?.Name;
            ViewBag.Section = student.SectionObj?.Name;
            ViewBag.StudentId = student.Id;
            ViewBag.RegistrationDate = student.RegistrationDate;
            ViewBag.CampusId = student.CampusId;

            return View(attendanceHistory);
        }

        [Authorize(Roles = "Admin,Teacher")]
        // GET: Attendance/Summary?classId=1&sectionId=2&date=2024-01-15
        public async Task<IActionResult> Summary(int? classId, int? sectionId, DateTime? date)
        {
            // Add null checks for parameters
            if (!classId.HasValue || !sectionId.HasValue)
            {
                TempData["ErrorMessage"] = "Class and section are required.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }
             
            var targetDate = date ?? DateTime.Today;

            // Get class and section names for display
            var selectedClass = await _context.Classes.FindAsync(classId.Value);
            var selectedSection = await _context.ClassSections.FindAsync(sectionId.Value);
            var selectedCampus = selectedClass.CampusId;

            if (selectedClass == null || selectedSection == null)
            {
                TempData["ErrorMessage"] = "Class or section not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get attendance records for the specific date
            var attendanceRecords = await _context.Attendance
                .Include(a => a.Student)
                .Where(a => a.Date == targetDate && a.ClassId == classId.Value && a.SectionId == sectionId.Value)
                .OrderBy(a => a.Student.StudentName)
                .ToListAsync();

            // Get summary statistics
            var summary = await _context.Attendance
                .Where(a => a.Date == targetDate && a.ClassId == classId.Value && a.SectionId == sectionId.Value )
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var totalStudents = await _context.Students
                .CountAsync(s => s.Class == classId.Value && s.Section == sectionId.Value && !s.HasLeft );
            ViewBag.CampusId = selectedCampus;
            ViewBag.classId = classId.Value;
            ViewBag.sectionId = sectionId.Value;
            ViewBag.Class = selectedClass.Name;
            ViewBag.Section = selectedSection.Name;
            ViewBag.Date = targetDate;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.Present = summary.FirstOrDefault(s => s.Status == "P")?.Count ?? 0;
            ViewBag.Absent = summary.FirstOrDefault(s => s.Status == "A")?.Count ?? 0;
            ViewBag.Leave = summary.FirstOrDefault(s => s.Status == "L")?.Count ?? 0;
            ViewBag.Late = summary.FirstOrDefault(s => s.Status == "T")?.Count ?? 0;

            return View(attendanceRecords);
        }
        [Authorize(Roles = "Admin")]
        // GET: Attendance/DailySummary?date=2024-01-15
        public async Task<IActionResult> DailySummary(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var dailySummary = await _context.Attendance
                .Include(a => a.ClassObj)
                .Include(a => a.SectionObj)
                .Where(a => a.Date == targetDate && (campusId == null || a.CampusId == campusId))
                .GroupBy(a => new { a.ClassId, a.SectionId, ClassName = a.ClassObj.Name, SectionName = a.SectionObj.Name })
                .Select(g => new DailyAttendanceSummaryViewModel
                {
                    ClassID = g.Key.ClassId,        // Changed to match ViewModel
                    SectionID = g.Key.SectionId,    // Changed to match ViewModel
                    Class = g.Key.ClassName,
                    Section = g.Key.SectionName,
                    Present = g.Count(a => a.Status == "P"),
                    Absent = g.Count(a => a.Status == "A"),
                    Leave = g.Count(a => a.Status == "L"),
                    Late = g.Count(a => a.Status == "T"),
                    TotalStudents = _context.Students.Count(s => s.Class == g.Key.ClassId && s.Section == g.Key.SectionId && !s.HasLeft && (campusId == null || s.CampusId == campusId))
                })
                .OrderBy(d => d.Class)
                .ThenBy(d => d.Section)
                .ToListAsync();

            ViewBag.Date = targetDate;
            return View(dailySummary);
        }
        // Helper view model for daily summary (separate from main models)

    }
}