using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize]
    public class MailBoxController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MailBoxController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// MailBox main view (Index)
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Compose message view
        /// </summary>
        public async Task<IActionResult> Compose()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get data for dropdowns
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Classes = await _context.Classes.Where(c => c.CampusId == currentUser.CampusId).OrderBy(c => c.Name).ToListAsync();

            return View();
        }

        /// <summary>
        /// AJAX: Get sections by class
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(cs => cs.ClassId == classId)
                .OrderBy(cs => cs.Name)
                .Select(cs => new { id = cs.Id, name = cs.Name })
                .ToListAsync();

            return Json(sections);
        }

        /// <summary>
        /// AJAX: Get students by class and section
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetStudentsBySection(int classId, int sectionId)
        {
            var students = await _context.Students
                .Where(s => s.Class == classId && s.Section == sectionId)
                .OrderBy(s => s.StudentName)
                .ToListAsync();

            var studentIds = students.Select(s => s.Id).ToList();

            var users = await _context.Users
                .Where(u => u.StudentId.HasValue && studentIds.Contains(u.StudentId.Value))
                .ToListAsync();

            var result = students.Select(s => {
                var user = users.FirstOrDefault(u => u.StudentId == s.Id);
                return new
                {
                    userId = user?.Id,
                    name = s.StudentName,
                    rollNumber = s.RollNumber
                };
            }).Where(s => s.userId != null).ToList();

            return Json(result);
        }

        /// <summary>
        /// AJAX: Get employees grouped by role
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetEmployees(int? campusId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var effectiveCampusId = campusId ?? currentUser?.CampusId ?? 0;

            var employees = await _context.Employees
                .Where(e => e.CampusId == effectiveCampusId)
                .OrderBy(e => e.Role)
                .ThenBy(e => e.FullName)
                .ToListAsync();

            var employeeIds = employees.Select(e => e.Id).ToList();

            var users = await _context.Users
                .Where(u => u.EmployeeId.HasValue && employeeIds.Contains(u.EmployeeId.Value))
                .ToListAsync();

            var employeeList = employees.Select(e => {
                var user = users.FirstOrDefault(u => u.EmployeeId == e.Id);
                return new
                {
                    userId = user?.Id,
                    name = e.FullName,
                    role = e.Role,
                    email = user?.Email
                };
            }).Where(e => e.userId != null).ToList();

            // Group by role
            var groupedEmployees = employeeList
                .GroupBy(e => e.role)
                .Select(g => new
                {
                    role = g.Key,
                    employees = g.ToList()
                })
                .ToList();

            return Json(groupedEmployees);
        }

        /// <summary>
        /// AJAX: Get admins by campus
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetAdminsByCampus(int campusId)
        {
            var adminEmployees = await _context.Employees
                .Where(e => e.Role == "Admin" && e.CampusId == campusId)
                .ToListAsync();

            var adminEmployeeIds = adminEmployees.Select(e => e.Id).ToList();

            var users = await _context.Users
                .Where(u => u.EmployeeId.HasValue && adminEmployeeIds.Contains(u.EmployeeId.Value))
                .ToListAsync();

            var result = adminEmployees.Select(e => {
                var user = users.FirstOrDefault(u => u.EmployeeId == e.Id);
                return new
                {
                    userId = user?.Id,
                    name = e.FullName,
                    email = user?.Email
                };
            }).Where(a => a.userId != null).ToList();

            return Json(result);
        }
    }
}
