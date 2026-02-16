using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using Microsoft.AspNetCore.Identity;

namespace SMS.Controllers
{
    public class AssignedDutiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignedDutiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AssignedDuties
        public async Task<IActionResult> Index(string searchString, string statusFilter, string employeeFilter, string dutyTypeFilter, bool viewAll = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["EmployeeFilter"] = employeeFilter;
            ViewData["DutyTypeFilter"] = dutyTypeFilter;
            ViewData["ViewAll"] = viewAll;

            // Dropdown options
            ViewBag.StatusOptions = new List<string> { "Assigned", "In Progress", "Completed", "Overdue", "Cancelled" };
            ViewBag.DutyTypeOptions = new List<string> { "Daily", "Weekly", "Monthly", "Special", "Event-based" };

            // Employee options for dropdown
            var employeeQuery = _context.Employees.Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                employeeQuery = employeeQuery.Where(e => e.CampusId == campusId);
            }
            ViewBag.EmployeeOptions = await employeeQuery.ToListAsync();

            var dutiesQuery = _context.AssignedDuties
                .Include(a => a.Employee)
                .Include(a => a.Campus)
                .Where(a => a.IsActive);

            // Apply campus filter
            if (campusId.HasValue && campusId > 0)
            {
                dutiesQuery = dutiesQuery.Where(a => a.CampusId == campusId);
            }

            // Default filter: Hide Completed, Overdue, and Cancelled duties unless viewAll is true
            if (!viewAll)
            {
                dutiesQuery = dutiesQuery.Where(a => a.Status != "Completed" && a.Status != "Overdue" && a.Status != "Cancelled");
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                dutiesQuery = dutiesQuery.Where(a => 
                    a.DutyTitle.Contains(searchString) ||
                    (a.Description != null && a.Description.Contains(searchString)) ||
                    (a.Employee != null && a.Employee.FullName.Contains(searchString)));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                dutiesQuery = dutiesQuery.Where(a => a.Status == statusFilter);
            }

            // Apply employee filter
            if (!string.IsNullOrEmpty(employeeFilter))
            {
                if (int.TryParse(employeeFilter, out int employeeId))
                {
                    dutiesQuery = dutiesQuery.Where(a => a.EmployeeId == employeeId);
                }
            }

            // Apply duty type filter
            if (!string.IsNullOrEmpty(dutyTypeFilter))
            {
                dutiesQuery = dutiesQuery.Where(a => a.DutyType == dutyTypeFilter);
            }

            var duties = await dutiesQuery
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();

            return View(duties);
        }

        // GET: AssignedDuties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignedDuty = await _context.AssignedDuties
                .Include(a => a.Employee)
                .Include(a => a.Campus)
                .Include(a => a.AssignedStudents)
                    .ThenInclude(ads => ads.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(a => a.AssignedStudents)
                    .ThenInclude(ads => ads.Student)
                        .ThenInclude(s => s.SectionObj)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignedDuty == null)
            {
                return NotFound();
            }

            // Check permissions: Only Admins and assigned Teachers/Students can view
            var currentUser = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(currentUser);
            bool isAdmin = roles.Contains("Admin");
            bool isAssignedEmployee = assignedDuty.EmployeeId.HasValue && assignedDuty.EmployeeId == currentUser.EmployeeId;
            bool isAssignedStudent = currentUser.StudentId.HasValue && 
                                     assignedDuty.AssignedStudents.Any(ads => ads.StudentId == currentUser.StudentId.Value && ads.IsActive);

            if (!isAdmin && !isAssignedEmployee && !isAssignedStudent)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this duty.";
                return RedirectToAction(nameof(Index));
            }

            // Pass permission flags to view for slider visibility
            ViewBag.IsAssignedEmployee = isAssignedEmployee || isAdmin;
            ViewBag.IsAssignedStudent = isAssignedStudent;

            return View(assignedDuty);
        }

        // GET: AssignedDuties/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var employeeQuery = _context.Employees.Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                employeeQuery = employeeQuery.Where(e => e.CampusId == campusId);
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive && c.Id == campusId),
                    "Id", "Name", campusId);
            }
            else
            {
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive),
                    "Id", "Name");
            }

            ViewData["EmployeeId"] = new SelectList(await employeeQuery.ToListAsync(), "Id", "FullName");
            ViewBag.DutyTypeOptions = new List<string> { "Daily", "Weekly", "Monthly", "Special", "Event-based" };
            ViewBag.PriorityOptions = new List<string> { "High", "Medium", "Low" };
            ViewBag.StatusOptions = new List<string> { "Assigned", "In Progress", "Completed", "Overdue", "Cancelled" };
            ViewBag.RecurrenceOptions = new List<string> { "Daily", "Weekly", "Monthly" };

            return View();
        }

        // POST: AssignedDuties/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,DutyTitle,Description,Priority,StartDate,DueDate,Instructions,CampusId")] AssignedDuty assignedDuty, int[] selectedStudents)
        {
            ModelState.Remove("Employee");
            ModelState.Remove("Campus");
            
            // Validate that at least one entity (Employee or Student) is assigned
            if (!assignedDuty.EmployeeId.HasValue && (selectedStudents == null || selectedStudents.Length == 0))
            {
                ModelState.AddModelError("", "At least one entity (Employee or Student) must be assigned to the duty.");
            }
            
            if (ModelState.IsValid)
            {
                assignedDuty.AssignedDate = DateTime.Now;
                assignedDuty.AssignedBy = User.Identity.Name;
                assignedDuty.IsActive = true;
                assignedDuty.ProgressPercentage = 0;
                assignedDuty.Status = "Assigned"; // Always set to Assigned on creation

                _context.Add(assignedDuty);
                await _context.SaveChangesAsync();

                // Add selected students to the duty
                if (selectedStudents != null && selectedStudents.Length > 0)
                {
                    foreach (var studentId in selectedStudents)
                    {
                        var assignedDutyStudent = new AssignedDutyStudent
                        {
                            AssignedDutyId = assignedDuty.Id,
                            StudentId = studentId,
                            AssignedDate = DateTime.Now,
                            Status = "Assigned",
                            IsActive = true
                        };
                        _context.AssignedDutyStudents.Add(assignedDutyStudent);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Duty assigned successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            await LoadDropdownData(assignedDuty);
            return View(assignedDuty);
        }

        // GET: AssignedDuties/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignedDuty = await _context.AssignedDuties
                .Include(a => a.AssignedStudents)
                .ThenInclude(ads => ads.Student)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (assignedDuty == null)
            {
                return NotFound();
            }

            await LoadDropdownData(assignedDuty);
            
            // Pass currently assigned student IDs to the view
            ViewBag.AssignedStudentIds = assignedDuty.AssignedStudents
                .Where(ads => ads.IsActive)
                .Select(ads => ads.StudentId)
                .ToArray();
                
            return View(assignedDuty);
        }

        // POST: AssignedDuties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmployeeId,DutyTitle,Description,Priority,StartDate,DueDate,Status,Instructions,ProgressPercentage,CampusId,AssignedBy,AssignedDate,IsActive")] AssignedDuty assignedDuty, int[] selectedStudents)
        {
            if (id != assignedDuty.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Employee");
            ModelState.Remove("Campus");
            
            // Validate that at least one entity (Employee or Student) is assigned
            if (!assignedDuty.EmployeeId.HasValue && (selectedStudents == null || selectedStudents.Length == 0))
            {
                ModelState.AddModelError("", "At least one entity (Employee or Student) must be assigned to the duty.");
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    assignedDuty.ModifiedBy = User.Identity.Name;
                    assignedDuty.ModifiedDate = DateTime.Now;

                    // If progress reaches 100%, automatically mark as completed
                    if (assignedDuty.ProgressPercentage >= 100)
                    {
                        assignedDuty.Status = "Completed";
                        assignedDuty.ProgressPercentage = 100;
                    }

                    _context.Update(assignedDuty);

                    // Update student assignments
                    // First, remove all existing assignments
                    var existingAssignments = await _context.AssignedDutyStudents
                        .Where(ads => ads.AssignedDutyId == id)
                        .ToListAsync();
                    _context.AssignedDutyStudents.RemoveRange(existingAssignments);

                    // Add new assignments
                    if (selectedStudents != null && selectedStudents.Length > 0)
                    {
                        foreach (var studentId in selectedStudents)
                        {
                            var assignedDutyStudent = new AssignedDutyStudent
                            {
                                AssignedDutyId = id,
                                StudentId = studentId,
                                AssignedDate = DateTime.Now,
                                Status = "Assigned",
                                IsActive = true
                            };
                            _context.AssignedDutyStudents.Add(assignedDutyStudent);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Duty updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssignedDutyExists(assignedDuty.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownData(assignedDuty);
            return View(assignedDuty);
        }

        // GET: AssignedDuties/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignedDuty = await _context.AssignedDuties
                .Include(a => a.Employee)
                .Include(a => a.Campus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignedDuty == null)
            {
                return NotFound();
            }

            return View(assignedDuty);
        }

        // POST: AssignedDuties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignedDuty = await _context.AssignedDuties.FindAsync(id);
            if (assignedDuty != null)
            {
                assignedDuty.IsActive = false; // Soft delete
                _context.Update(assignedDuty);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Duty deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownData(AssignedDuty assignedDuty)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var employeeQuery = _context.Employees.Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                employeeQuery = employeeQuery.Where(e => e.CampusId == campusId);
            }

            ViewData["EmployeeId"] = new SelectList(await employeeQuery.ToListAsync(), "Id", "FullName", assignedDuty.EmployeeId);
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", assignedDuty.CampusId);
            
            // Load classes and students for the dropdown
            var classQuery = _context.Classes.Where(c => c.IsActive);
            var studentQuery = _context.Students.Where(s => !s.HasLeft);
            
            if (campusId.HasValue && campusId > 0)
            {
                classQuery = classQuery.Where(c => c.CampusId == campusId);
                studentQuery = studentQuery.Where(s => s.CampusId == campusId);
            }
            
            ViewBag.Classes = await classQuery.ToListAsync();
            ViewBag.Students = await studentQuery.Include(s => s.ClassObj).ToListAsync();
            
            ViewBag.DutyTypeOptions = new List<string> { "Daily", "Weekly", "Monthly", "Special", "Event-based" };
            ViewBag.PriorityOptions = new List<string> { "High", "Medium", "Low" };
            ViewBag.StatusOptions = new List<string> { "Assigned", "In Progress", "Completed", "Overdue", "Cancelled" };
            ViewBag.RecurrenceOptions = new List<string> { "Daily", "Weekly", "Monthly" };
        }

        // GET: Get Students by Class
        [HttpGet]
        public async Task<JsonResult> GetStudentsByClass(int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var studentsQuery = _context.Students
                .Where(s => s.Class == classId && !s.HasLeft);

            if (campusId.HasValue && campusId > 0)
            {
                studentsQuery = studentsQuery.Where(s => s.CampusId == campusId);
            }

            var students = await studentsQuery
                .Select(s => new { id = s.Id, name = s.StudentName, fatherName = s.FatherName })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(students);
        }

        // GET: Get classes by campus (AJAX)
        [HttpGet]
        public async Task<JsonResult> GetClassesByCampus(int campusId)
        {
            var classes = await _context.Classes
                .Where(c => c.IsActive && c.CampusId == campusId)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();

            return Json(classes);
        }

        // POST: Get students by IDs (for editing)
        [HttpPost]
        public async Task<JsonResult> GetStudentsByIds([FromBody] int[] studentIds)
        {
            var students = await _context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.HasLeft)
                .Select(s => new { id = s.Id, name = s.StudentName })
                .ToListAsync();

            return Json(students);
        }

        // POST: Update Progress Percentage
        [HttpPost]
        public async Task<IActionResult> UpdateProgress(int id, int progressPercentage)
        {
            var assignedDuty = await _context.AssignedDuties.FindAsync(id);
            if (assignedDuty == null)
            {
                return Json(new { success = false, message = "Duty not found" });
            }

            // Verify permission - only assigned user or admin can update
            var currentUser = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(currentUser);
            bool isAdmin = roles.Contains("Admin");
            bool isAssignedEmployee = assignedDuty.EmployeeId.HasValue && assignedDuty.EmployeeId == currentUser.EmployeeId;
            
            var assignedStudents = await _context.AssignedDutyStudents
                .Where(ads => ads.AssignedDutyId == id && ads.IsActive)
                .ToListAsync();
            bool isAssignedStudent = currentUser.StudentId.HasValue && 
                                     assignedStudents.Any(ads => ads.StudentId == currentUser.StudentId.Value);

            if (!isAdmin && !isAssignedEmployee && !isAssignedStudent)
            {
                return Json(new { success = false, message = "You do not have permission to update this duty." });
            }

            // Check if duty is overdue or cancelled
            if (assignedDuty.Status == "Overdue" || assignedDuty.Status == "Cancelled")
            {
                return Json(new { success = false, message = "Cannot update progress for overdue or cancelled duties." });
            }

            assignedDuty.ProgressPercentage = progressPercentage;
            assignedDuty.ModifiedBy = User.Identity.Name;
            assignedDuty.ModifiedDate = DateTime.Now;

            // Update status based on progress
            if (progressPercentage >= 100)
            {
                assignedDuty.Status = "Completed";
                assignedDuty.ProgressPercentage = 100;
                assignedDuty.CompletedDate = DateTime.Now;
            }
            else if (progressPercentage > 0 && progressPercentage < 100)
            {
                assignedDuty.Status = "In Progress";
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Progress updated successfully", status = assignedDuty.Status });
        }

        // POST: Mark duty as Completed
        [HttpPost]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var assignedDuty = await _context.AssignedDuties.FindAsync(id);
            if (assignedDuty == null)
            {
                return Json(new { success = false, message = "Duty not found" });
            }

            assignedDuty.Status = "Completed";
            assignedDuty.ProgressPercentage = 100;
            assignedDuty.CompletedDate = DateTime.Now;
            assignedDuty.ModifiedBy = User.Identity.Name;
            assignedDuty.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Duty marked as completed" });
        }

        // POST: Mark duty as Cancelled
        [HttpPost]
        public async Task<IActionResult> MarkAsCancelled(int id)
        {
            var assignedDuty = await _context.AssignedDuties.FindAsync(id);
            if (assignedDuty == null)
            {
                return Json(new { success = false, message = "Duty not found" });
            }

            assignedDuty.Status = "Cancelled";
            assignedDuty.ModifiedBy = User.Identity.Name;
            assignedDuty.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Duty marked as cancelled" });
        }

        // POST: Hard delete duty
        [HttpPost]
        public async Task<IActionResult> HardDelete(int id)
        {
            var assignedDuty = await _context.AssignedDuties
                .Include(a => a.AssignedStudents)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (assignedDuty == null)
            {
                return Json(new { success = false, message = "Duty not found" });
            }

            // Remove all associated student assignments first
            if (assignedDuty.AssignedStudents.Any())
            {
                _context.AssignedDutyStudents.RemoveRange(assignedDuty.AssignedStudents);
            }

            // Remove the duty itself
            _context.AssignedDuties.Remove(assignedDuty);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Duty deleted successfully" });
        }

        // GET: Filter duties with AJAX
        [HttpGet]
        public async Task<IActionResult> FilterDuties(string searchString, string statusFilter, string employeeFilter, DateTime? dateFilter, bool viewAll = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var dutiesQuery = _context.AssignedDuties
                .Include(a => a.Employee)
                .Include(a => a.Campus)
                .Include(a => a.AssignedStudents)
                    .ThenInclude(ads => ads.Student)
                .Where(a => a.IsActive);

            // Apply campus filter
            if (campusId.HasValue && campusId > 0)
            {
                dutiesQuery = dutiesQuery.Where(a => a.CampusId == campusId);
            }

            // Default filter: Hide Completed, Overdue, and Cancelled duties unless viewAll is true
            if (!viewAll)
            {
                dutiesQuery = dutiesQuery.Where(a => a.Status != "Completed" && a.Status != "Overdue" && a.Status != "Cancelled");
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                dutiesQuery = dutiesQuery.Where(a => 
                    a.DutyTitle.Contains(searchString) ||
                    (a.Description != null && a.Description.Contains(searchString)) ||
                    (a.Employee != null && a.Employee.FullName.Contains(searchString)));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                dutiesQuery = dutiesQuery.Where(a => a.Status == statusFilter);
            }

            // Apply employee filter
            if (!string.IsNullOrEmpty(employeeFilter))
            {
                if (int.TryParse(employeeFilter, out int employeeId))
                {
                    dutiesQuery = dutiesQuery.Where(a => a.EmployeeId == employeeId);
                }
            }

            // Apply date filter
            if (dateFilter.HasValue)
            {
                dutiesQuery = dutiesQuery.Where(a => a.DueDate.Date == dateFilter.Value.Date);
            }

            var duties = await dutiesQuery
                .OrderByDescending(a => a.AssignedDate)
                .Select(a => new
                {
                    id = a.Id,
                    dutyTitle = a.DutyTitle,
                    description = a.Description,
                    priority = a.Priority,
                    status = a.Status,
                    progressPercentage = a.ProgressPercentage,
                    employeeName = a.Employee != null ? a.Employee.FullName : "No Employee Assigned",
                    startDate = a.StartDate.ToString("MMM dd, yyyy"),
                    dueDate = a.DueDate.ToString("MMM dd, yyyy"),
                    studentNames = a.AssignedStudents
                        .Where(ads => ads.IsActive)
                        .Select(ads => ads.Student.StudentName)
                        .ToList()
                })
                .ToListAsync();

            return Json(new { success = true, duties });
        }

        // Background job method to update duty statuses
        public async Task UpdateDutyStatuses()
        {
            var today = DateTime.Today;

            // Update duties to "In Progress" if Start Date has arrived and status is "Assigned"
            var dutiesToStart = await _context.AssignedDuties
                .Where(d => d.IsActive && 
                            d.Status == "Assigned" && 
                            d.StartDate <= today)
                .ToListAsync();

            foreach (var duty in dutiesToStart)
            {
                duty.Status = "In Progress";
                // Progress percentage already initialized to 0 in model
            }

            // Update duties to "Overdue" if Due Date has passed and Progress < 100%
            var dutiesToOverdue = await _context.AssignedDuties
                .Where(d => d.IsActive && 
                            d.Status != "Completed" && 
                            d.Status != "Cancelled" &&
                            d.DueDate < today && 
                            d.ProgressPercentage < 100)
                .ToListAsync();

            foreach (var duty in dutiesToOverdue)
            {
                duty.Status = "Overdue";
            }

            await _context.SaveChangesAsync();
        }

        private bool AssignedDutyExists(int id)
        {
            return _context.AssignedDuties.Any(e => e.Id == id);
        }
    }
}