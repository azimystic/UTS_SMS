using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SMS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        
        // Common role groups - static to avoid recreating on each request
        private static readonly string[] AllAuthenticatedRoles = { "Admin", "Teacher", "Student", "Accountant", "Parent", "Owner" };
        private static readonly string[] AdminAccountantRoles = { "Admin", "Accountant" };
        private static readonly string[] AdminTeacherRoles = { "Admin", "Teacher" };
        
        // Controller authorization mappings - static to avoid recreating on each request
        private static readonly Dictionary<string, string[]> ControllerAuthorization = new()
        {
            // Admin only controllers
            { "AcademicCalendar", new[] { "Admin" } },
            { "Admin", new[] { "Admin" } },
            { "Asset", new[] { "Admin" } },
            { "Employees", new[] { "Admin" } },
            { "Expense", new[] { "Admin" } },
            { "StudentCategory", new[] { "Admin" } },
            { "StudentFineCharges", new[] { "Admin" } },
            { "StudentMigration", new[] { "Admin" } },
            { "SurveyQuestion", new[] { "Admin" } },
            { "TeacherPerformance", new[] { "Admin" } },
            { "PayrollReports", new[] { "Admin" } },
            { "ManageDefinition", new[] { "Admin" } },
            { "AdmissionInquiries", new[] { "Admin" } },
            { "AssignedDuties", new[] { "Admin" } },
            { "Campuses", new[] { "Admin" } },
            { "ClassSections", new[] { "Admin" } },
            { "Classes", new[] { "Admin" } },
            { "ExamCategories", new[] { "Admin" } },
            { "Exams", new[] { "Admin" } },
            { "StudentPosition", new[] { "Admin" } },
            { "StudentPromotion", new[] { "Admin" } },
            { "Subjects", new[] { "Admin" } },
            { "SubjectsGroupings", new[] { "Admin" } },
            { "TeacherAssignments", new[] { "Admin" } },
            
            // Admin, Teacher controllers
            { "Students", AdminTeacherRoles },
            { "Family", AdminTeacherRoles },
            { "Teacher", AdminTeacherRoles },
            { "TestReturn", AdminTeacherRoles },
            { "ExamMarks", AdminTeacherRoles },
            { "ExamReports", AdminTeacherRoles },
            { "Timetables", AdminTeacherRoles },
            
            // Admin, Accountant controllers
            { "Billing", AdminAccountantRoles },
            { "Payroll", AdminAccountantRoles },
            { "BankAccounts", AdminAccountantRoles },
            { "ClassFee", AdminAccountantRoles },
            { "SalaryDefinition", AdminAccountantRoles },
            
            // Admin, Accountant, Student controllers (for viewing)
            { "BillingReports", new[] { "Admin", "Accountant", "Student" } },
            
            // Admin, Owner controllers
            { "CalendarEvents", new[] { "Admin", "Owner" } },
            
            // Admin, Student controllers
            { "StudentDashboard", new[] { "Admin", "Student" } },
            { "StudentComplaint", new[] { "Admin", "Student" } },
            
            // Student only controllers
            { "StudentSurvey", new[] { "Student" } },
            
            // Owner only controllers
            { "Owner", new[] { "Owner" } },
            
            // Parent controllers
            { "ParentDashboard", new[] { "Parent", "Admin" } },
            
            // All authenticated users (available to everyone logged in)
            { "Attendance", AllAuthenticatedRoles },
            { "Diary", AllAuthenticatedRoles },
            { "EmployeeAttendance", AllAuthenticatedRoles },
            { "ExamDateSheet", AllAuthenticatedRoles },
            { "NamazAttendance", AllAuthenticatedRoles },
            { "Profile", AllAuthenticatedRoles },
            { "Account", AllAuthenticatedRoles },
            { "Home", AllAuthenticatedRoles }
        };

        public SearchController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("views")]
        public IActionResult GetAllViews()
        {
            try
            {
                var viewsPath = Path.Combine(_env.ContentRootPath, "Views");
                
                // Validate that the Views directory exists
                if (!Directory.Exists(viewsPath))
                {
                    return Ok(new List<object>());
                }
                
                // Get user roles
                var userRoles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToHashSet(); // Use HashSet for O(1) lookups
                
                var controllerViewData = new List<object>();

                // Get all controller directories
                var controllerDirs = Directory.GetDirectories(viewsPath)
                    .Where(d => !Path.GetFileName(d).StartsWith("_") && 
                               Path.GetFileName(d) != "Shared")
                    .OrderBy(d => Path.GetFileName(d));

                foreach (var dir in controllerDirs)
                {
                    var controllerName = Path.GetFileName(dir);
                    
                    // Check if user is authorized to access this controller
                    if (!IsUserAuthorizedForController(controllerName, userRoles))
                    {
                        continue;
                    }
                    
                    // Validate that we're still within the Views directory
                    if (!dir.StartsWith(viewsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    // Get all view files in this controller directory
                    var viewFiles = Directory.GetFiles(dir, "*.cshtml")
                        .Select(f => Path.GetFileNameWithoutExtension(f))
                        .Where(v => !v.StartsWith("_") && 
                                   !v.Equals("Edit", StringComparison.OrdinalIgnoreCase) && 
                                   !v.Equals("Details", StringComparison.OrdinalIgnoreCase) &&
                                   !v.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(v => v)
                        .ToList();

                    if (viewFiles.Any())
                    {
                        controllerViewData.Add(new
                        {
                            Controller = controllerName,
                            Views = viewFiles
                        });
                    }
                }

                return Ok(controllerViewData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to load views data" });
            }
        }

        private bool IsUserAuthorizedForController(string controllerName, HashSet<string> userRoles)
        {
            // Check if controller has specific authorization requirements
            if (ControllerAuthorization.TryGetValue(controllerName, out var allowedRoles))
            {
                // Use intersection for O(n) complexity instead of nested loops
                return allowedRoles.Any(role => userRoles.Contains(role));
            }
            
            // Default: deny access for unknown controllers
            return false;
        }
    }
}
