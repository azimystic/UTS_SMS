using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Models;
using SMS.ViewModels;
using System.Diagnostics;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentSurveyAnswerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentSurveyAnswerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentSurveyAnswer
        public async Task<IActionResult> Index(int? employeeId = null, int? month = null, int? year = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            // Default to current month and year if not specified
            if (!month.HasValue || !year.HasValue)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }

            // Get all teachers for the dropdown
            var teachersQuery = _context.Employees
                .Where(e => e.IsActive && e.Role == "Teacher");

            if (campusId.HasValue)
            {
                teachersQuery = teachersQuery.Where(e => e.CampusId == campusId.Value);
            }

            var teachers = await teachersQuery
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.Teachers = teachers;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.IsCurrentMonth = month == DateTime.Now.Month && year == DateTime.Now.Year;

            // If no employee is selected, show search form only
            if (!employeeId.HasValue)
            {
                return View();
            }

            // Get employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId.Value && e.IsActive);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Teacher not found";
                return RedirectToAction(nameof(Index));
            }

            // Get all teacher assignments for this employee
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == employee.Id && ta.IsActive)
                .Include(ta => ta.Subject)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .ToListAsync();

            Debug.WriteLine($"[DEBUG] Teacher Assignments Count: {teacherAssignments.Count}");

            // Get survey responses for selected month/year
            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            Debug.WriteLine($"[DEBUG] Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // Get only questions that were created before or during the selected month/year
            var eligibleQuestionIds = await _context.SurveyQuestions
                .Where(sq => sq.IsActive && 
                             (sq.CreatedDate.Year < year.Value || 
                             (sq.CreatedDate.Year == year.Value && sq.CreatedDate.Month <= month.Value)))
                .Select(sq => sq.Id)
                .ToListAsync();

            Debug.WriteLine($"[DEBUG] Eligible Question IDs: {string.Join(", ", eligibleQuestionIds)}");

            var surveyResponses = await _context.StudentSurveyResponses
                .Where(ssr => ssr.TeacherId == employee.Id && 
                             ssr.ResponseDate >= startDate && 
                             ssr.ResponseDate <= endDate &&
                             eligibleQuestionIds.Contains(ssr.SurveyQuestionId))
                .Include(ssr => ssr.SurveyQuestion)
                .Include(ssr => ssr.Student)
                    .ThenInclude(s => s.ClassObj)
                .Include(ssr => ssr.Student)
                    .ThenInclude(s => s.SectionObj)
                .ToListAsync();

            Debug.WriteLine($"[DEBUG] Survey Responses Count: {surveyResponses.Count}");

            // Get unique student IDs who have submitted at least one response
            var respondedStudentIds = surveyResponses
                .Select(r => r.StudentId)
                .Distinct()
                .ToHashSet();

            Debug.WriteLine($"[DEBUG] Responded Student IDs: {string.Join(", ", respondedStudentIds)}");

            // Calculate analytics using ViewModels
            var questionAverages = surveyResponses
                .GroupBy(r => new { r.SurveyQuestionId, r.SurveyQuestion.QuestionText, r.SurveyQuestion.QuestionOrder })
                .Select(g => new QuestionAverageViewModel
                {
                    QuestionText = g.Key.QuestionText,
                    QuestionOrder = g.Key.QuestionOrder,
                    AverageScore = g.Count() > 0 ? (g.Count(r => r.Response) * 100.0 / g.Count()) : 0,
                    TotalResponses = g.Count()
                })
                .OrderBy(q => q.QuestionOrder)
                .ToList();

            Debug.WriteLine($"[DEBUG] Question Averages Count: {questionAverages.Count}");
            foreach (var qa in questionAverages)
            {
                Debug.WriteLine($"[DEBUG] Q{qa.QuestionOrder}: {qa.QuestionText} - Score: {qa.AverageScore:F1}% - Responses: {qa.TotalResponses}");
            }

            var analytics = new SurveyAnalyticsViewModel
            {
                TotalSubmissions = respondedStudentIds.Count,
                QuestionAverages = questionAverages
            };

            // Find students who should have filled the survey but didn't
            // Get all unique class/section combinations from teacher assignments
            var classSectionPairs = teacherAssignments
                .Select(ta => new { ClassId = ta.ClassId, SectionId = ta.SectionId })
                .Distinct()
                .ToList();
            
            Debug.WriteLine($"[DEBUG] Class/Section Pairs: {classSectionPairs.Count}");

            if (!classSectionPairs.Any())
            {
                ViewBag.Employee = employee;
                ViewBag.Analytics = analytics;
                ViewBag.MissingStudents = new List<MissingStudentViewModel>();
                return View();
            }

            // Fetch all students for these class/sections in one optimized query
            var allStudents = await _context.Students
                .Where(s => s.HasLeft == false && s.RegistrationDate <= endDate)
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .ToListAsync();
            
            Debug.WriteLine($"[DEBUG] All Students Count: {allStudents.Count}");

            // Filter students to only those in teacher's class/sections
            var relevantStudents = allStudents
                .Where(s => classSectionPairs.Any(cs => cs.ClassId == s.Class && cs.SectionId == s.Section))
                .ToList();
            
            Debug.WriteLine($"[DEBUG] Relevant Students Count: {relevantStudents.Count}");

            // Find students who haven't responded to ANY question
            var missingStudents = relevantStudents
                .Where(s => !respondedStudentIds.Contains(s.Id))
                .Select(s => new MissingStudentViewModel
                {
                    StudentId = s.Id,
                    StudentName = s.StudentName,
                    ClassName = s.ClassObj.Name,
                    SectionName = s.SectionObj.Name,
                    RollNo = s.RollNumber
                })
                .OrderBy(s => s.ClassName)
                .ThenBy(s => s.SectionName)
                .ThenBy(s => s.RollNo)
                .ToList();

            Debug.WriteLine($"[DEBUG] Missing Students Count: {missingStudents.Count}");
            foreach (var ms in missingStudents)
            {
                Debug.WriteLine($"[DEBUG] Missing: {ms.StudentName} (ID: {ms.StudentId}, Roll: {ms.RollNo}, Class: {ms.ClassName}-{ms.SectionName})");
            }

            ViewBag.Employee = employee;
            ViewBag.Analytics = analytics;
            ViewBag.MissingStudents = missingStudents;

            return View();
        }
    }
}
