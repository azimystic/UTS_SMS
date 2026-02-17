using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWhatsAppService _whatsAppService;

        public EmployeesController(ApplicationDbContext context, IWebHostEnvironment env, IUserService userService, UserManager<ApplicationUser> userManager, IWhatsAppService whatsAppService)
        {
            _context = context;
            _env = env;
            _userService = userService;
            _userManager = userManager;
            _whatsAppService = whatsAppService;
        }

        // GET: Employees
        public async Task<IActionResult> Index(
            string sortOrder,
            string currentFilter,
            string searchString,
            string roleFilter,
            bool? showInactive,
            int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["RoleSortParm"] = sortOrder == "Role" ? "role_desc" : "Role";

            ViewBag.Roles = await _context.Employees
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["RoleFilter"] = roleFilter;
            ViewData["ShowInactive"] = showInactive;

            var employees = from e in _context.Employees
                            select e;

            // Filter by active status
            if (showInactive.HasValue)
            {
                employees = employees.Where(e => e.IsActive == !showInactive);
            }

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    e.FullName.Contains(searchString) ||
                    e.CNIC.Contains(searchString) ||
                    e.PhoneNumber.Contains(searchString) ||
                    e.Role.Contains(searchString));
            }

            // Role filter
            if (!string.IsNullOrEmpty(roleFilter))
            {
                employees = employees.Where(e => e.Role == roleFilter);
            }

            // Sorting
            switch (sortOrder)
            {
                case "name_desc":
                    employees = employees.OrderByDescending(e => e.FullName);
                    break;
                case "Date":
                    employees = employees.OrderBy(e => e.JoiningDate);
                    break;
                case "date_desc":
                    employees = employees.OrderByDescending(e => e.JoiningDate);
                    break;
                case "Role":
                    employees = employees.OrderBy(e => e.Role);
                    break;
                case "role_desc":
                    employees = employees.OrderByDescending(e => e.Role);
                    break;
                default:
                    employees = employees.OrderBy(e => e.FullName);
                    break;
            }

            int pageSize = 10;
            return View(await PaginatedList<Employee>.CreateAsync(employees.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsInactive(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = false;
            
            // Cascade inactivity to TeacherAssignments if the employee is a Teacher
            if (employee.Role == "Teacher")
            {
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == id && ta.IsActive)
                    .ToListAsync();
                
                foreach (var assignment in teacherAssignments)
                {
                    assignment.IsActive = false;
                }
            }
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = employee.Role == "Teacher" 
                ? "Employee and associated teacher assignments marked as inactive successfully!"
                : "Employee marked as inactive successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsActive(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee marked as active successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Create
        public async Task<IActionResult> CreateAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name");
                ViewData["EmployeeRoleConfigs"] = new SelectList(_context.EmployeeRoleConfigs.Where(erc => erc.IsActive && erc.CampusId == campusId), "Id", "RoleName");
             }
            else
            {
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name");
                ViewData["EmployeeRoleConfigs"] = new SelectList(_context.EmployeeRoleConfigs.Where(erc => erc.IsActive), "Id", "RoleName");
 
            } 
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee,
            int? employeeRoleConfigId,
            IFormFile profilePicture,
            IFormFile cnicFront,
            IFormFile cnicBack,
            IFormFile matricCertificate,
            IFormFile interCertificate,
            IFormFile degreeCertificate,
            IFormFile otherQualifications)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            ModelState.Remove(nameof(Employee.RegisteredBy));
            ModelState.Remove(nameof(Employee.CNIC_Back));
            ModelState.Remove(nameof(Employee.CNIC_Front));
            // Remove image fields from ModelState validation
            ModelState.Remove("ProfilePicture");
            ModelState.Remove("cnicFront");
            ModelState.Remove("cnicBack");
            ModelState.Remove("MatricCertificate");
            ModelState.Remove("InterCertificate");
            ModelState.Remove("DegreeCertificate");
            ModelState.Remove("OtherQualifications");
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                try
                {
                    var campus = _context.Campuses.FirstOrDefault(cs => cs.IsActive && cs.Id == employee.CampusId);
                    if (campus != null)
                    {
                        if (employee.OnTime == null)
                        {
                            employee.OnTime = campus.StartTime;
                        }
                        if (employee.OffTime == null)
                        {
                            employee.OffTime = campus.EndTime; // Fixed: should be OffTime, not OnTime
                        }
                    }
                    // Upload files
                    employee.ProfilePicture = await employee.UploadFile(profilePicture, "employee-profile-pictures", _env);
                    employee.CNIC_Front = await employee.UploadFile(cnicFront, "employee-cnic", _env);
                    employee.CNIC_Back = await employee.UploadFile(cnicBack, "employee-cnic", _env);
                    employee.MatricCertificate = await employee.UploadFile(matricCertificate, "employee-certificates", _env);
                    employee.InterCertificate = await employee.UploadFile(interCertificate, "employee-certificates", _env);
                    employee.DegreeCertificate = await employee.UploadFile(degreeCertificate, "employee-certificates", _env);
                    employee.OtherQualifications = await employee.UploadFile(otherQualifications, "employee-certificates", _env);
                    employee.CampusId = employee.CampusId;
                    // Set additional fields
                    employee.RegisteredBy = User.Identity.Name;
                    employee.JoiningDate = DateTime.Now;

                    _context.Add(employee);
                    await _context.SaveChangesAsync();
                    
                    // Create EmployeeRole record if role config is selected
                    if (employeeRoleConfigId.HasValue && employeeRoleConfigId.Value > 0)
                    {
                        var employeeRole = new EmployeeRole
                        {
                            EmployeeId = employee.Id,
                            EmployeeRoleConfigId = employeeRoleConfigId.Value,
                            FromDate = employee.JoiningDate,
                            IsActive = true,
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now,
                            CampusId = employee.CampusId
                        };
                        _context.EmployeeRoles.Add(employeeRole);
                        await _context.SaveChangesAsync();

                        // Create leave balances for this employee based on role configs
                        await CreateLeaveBalancesForEmployee(employee.Id, employeeRoleConfigId.Value, employee.CampusId, User.Identity.Name);
                    }
                    
                    // Create user account for the student
                    if(employee.Role == "Teacher")
                    {
                        var teacherResult = await _userService.CreateEmployeeUserAsync(employee);
                        if (teacherResult.Succeeded)
                        {
                            // Update student with user creation info
                            _context.Update(employee);
                            await _context.SaveChangesAsync();

                            // Send WhatsApp with credentials (fire-and-forget)
                            var username = employee.CNIC.Replace("-", "");
                            var password = "employee123";
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(employee.PhoneNumber))
                                    {
                                        var message = $"Welcome {employee.FullName}! Your account is created. Username: {username} Password: {password}.";
                                        await _whatsAppService.SendMessageAsync(
                                            employee.FullName,
                                            employee.PhoneNumber,
                                            message
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error sending WhatsApp: {ex.Message}");
                                }
                            });

                            TempData["Success"] = $"Employee and user account created successfully! Username: {username}, Password: {password}";
                        }
                        else
                        {
                            TempData["Warning"] = "Employee created but user account creation failed. Please create manually.";
                        }
                    }
                    if (employee.Role == "Accountant")
                    {
                        var accountantResult = await _userService.CreateAccountantUserAsync(employee);
                        if (accountantResult.Succeeded)
                        {
                            // Update student with user creation info
                            _context.Update(employee);
                            await _context.SaveChangesAsync();

                            // Send WhatsApp with credentials (fire-and-forget)
                            var username = employee.CNIC.Replace("-", "");
                            var password = "employee123";
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(employee.PhoneNumber))
                                    {
                                        var message = $"Welcome {employee.FullName}! Your account is created. Username: {username} Password: {password}.";
                                        await _whatsAppService.SendMessageAsync(
                                            employee.FullName,
                                            employee.PhoneNumber,
                                            message
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error sending WhatsApp: {ex.Message}");
                                }
                            });

                            TempData["Success"] = $"Employee and user account created successfully! Username: {username}, Password: {password}";
                        }
                        else
                        {
                            TempData["Warning"] = "Employee created but user account creation failed. Please create manually.";
                        }
                    }
                    

                    

                    TempData["SuccessMessage"] = "Employee registered successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }
            if (campusId == null || campusId == 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name");
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["EmployeeRoleConfigs"] = new SelectList(_context.EmployeeRoleConfigs.Where(erc => erc.IsActive), "Id", "RoleName");
            }
            else
            {
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name");
                ViewData["EmployeeRoleConfigs"] = new SelectList(_context.EmployeeRoleConfigs.Where(erc => erc.IsActive && erc.CampusId == campusId), "Id", "RoleName");
             } 
            return View(employee);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Campus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name");
            }
            else
            {
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name");

            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee,
            IFormFile profilePicture,
            IFormFile cnicFront,
            IFormFile cnicBack,
            IFormFile matricCertificate,
            IFormFile interCertificate,
            IFormFile degreeCertificate,
            IFormFile otherQualifications)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Employee.RegisteredBy));

            // Remove image fields from ModelState validation
            ModelState.Remove("ProfilePicture");
             ModelState.Remove("cnicFront");
            ModelState.Remove("cnicBack");
            ModelState.Remove("MatricCertificate");
            ModelState.Remove("InterCertificate");
            ModelState.Remove("DegreeCertificate");
            ModelState.Remove("OtherQualifications");
            ModelState.Remove("Campus");
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            if (ModelState.IsValid)
            {
                try
                {
                    var existingEmployee = await _context.Employees.FindAsync(id);
                    if (existingEmployee == null)
                    {
                        return NotFound();
                    }
                    var campus = _context.Campuses.FirstOrDefault(cs => cs.IsActive && cs.Id == employee.CampusId);
                    if (campus != null)
                    {
                        if (employee.OnTime == null)
                        {
                            employee.OnTime = campus.StartTime;
                        }
                        if (employee.OffTime == null)
                        {
                            employee.OffTime = campus.EndTime; // Fixed: should be OffTime, not OnTime
                        }
                    }
                    
                    // Check if CNIC has changed and update username accordingly
                    var oldCNIC = existingEmployee.CNIC;
                    var newCNIC = employee.CNIC;
                    var cnicChanged = oldCNIC != newCNIC;
                    
                    // Update properties
                    existingEmployee.FullName = employee.FullName;
                    existingEmployee.CNIC = employee.CNIC;
                    existingEmployee.Email = employee.Email;
                    existingEmployee.PhoneNumber = employee.PhoneNumber;
                    existingEmployee.Address = employee.Address;
                    existingEmployee.DateOfBirth = employee.DateOfBirth;
                    existingEmployee.Gender = employee.Gender;
                    existingEmployee.Role = employee.Role;
                    existingEmployee.EducationLevel = employee.EducationLevel;
                    existingEmployee.Degree = employee.Degree;
                    existingEmployee.MajorSubject = employee.MajorSubject;
                    existingEmployee.MatricMarks = employee.MatricMarks;
                    existingEmployee.InterMarks = employee.InterMarks;
                    existingEmployee.CGPA = employee.CGPA;
                    existingEmployee.University = employee.University;
                    existingEmployee.CampusId = employee.CampusId;
                    existingEmployee.OnTime = employee.OnTime;
                    existingEmployee.OffTime = employee.OffTime;

                    // Update file fields only if new files are provided
                    if (profilePicture != null && profilePicture.Length > 0)
                        existingEmployee.ProfilePicture = await UpdateFile(profilePicture, existingEmployee.ProfilePicture, "employee-profile-pictures");

                    if (cnicFront != null && cnicFront.Length > 0)
                        existingEmployee.CNIC_Front = await UpdateFile(cnicFront, existingEmployee.CNIC_Front, "employee-cnic");

                    if (cnicBack != null && cnicBack.Length > 0)
                        existingEmployee.CNIC_Back = await UpdateFile(cnicBack, existingEmployee.CNIC_Back, "employee-cnic");

                    if (matricCertificate != null && matricCertificate.Length > 0)
                        existingEmployee.MatricCertificate = await UpdateFile(matricCertificate, existingEmployee.MatricCertificate, "employee-certificates");

                    if (interCertificate != null && interCertificate.Length > 0)
                        existingEmployee.InterCertificate = await UpdateFile(interCertificate, existingEmployee.InterCertificate, "employee-certificates");

                    if (degreeCertificate != null && degreeCertificate.Length > 0)
                        existingEmployee.DegreeCertificate = await UpdateFile(degreeCertificate, existingEmployee.DegreeCertificate, "employee-certificates");

                    if (otherQualifications != null && otherQualifications.Length > 0)
                        existingEmployee.OtherQualifications = await UpdateFile(otherQualifications, existingEmployee.OtherQualifications, "employee-certificates");

                    await _context.SaveChangesAsync();
                    
                    // Update username if CNIC changed for Teacher or Accountant
                    if (cnicChanged && (existingEmployee.Role == "Teacher" || existingEmployee.Role == "Accountant"))
                    {
                        var usernameUpdated = await _userService.UpdateEmployeeUsernameAsync(existingEmployee.Id, newCNIC);
                        if (usernameUpdated)
                        {
                            TempData["SuccessMessage"] = "Employee and user account updated successfully!";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Employee updated successfully! (Username update not applicable)";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Employee updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (campusId == null || campusId == 0)
            {
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name");
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
            }
            else
            {
                ViewBag.Roles = GetRoleList();
                ViewBag.EducationLevels = GetEducationLevelList();
                ViewBag.Genders = GetGenderList();
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name");
            }
            return View(employee);
        }

        private async Task<string> UpdateFile(IFormFile newFile, string existingFilePath, string folderName)
        {
            if (newFile == null || newFile.Length == 0)
                return existingFilePath;

            // Delete old file if exists
            if (!string.IsNullOrEmpty(existingFilePath))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath, existingFilePath.Replace("/", "\\"));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Upload new file
            var employee = new Employee();
            return await employee.UploadFile(newFile, folderName, _env);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        private List<string> GetRoleList()
        {
            return new List<string> { "Teacher", "Accountant", "Admin", "Aya", "Guard", "Lab Instructor" };
        }

        private List<string> GetEducationLevelList()
        {
            return new List<string> { "Matric", "FSC", "BS", "BSc", "MSc", "PhD", "Other" };
        }

        private List<string> GetGenderList()
        {
            return new List<string> { "Male", "Female", "Other" };
        }

        // Helper method to create leave balances for a new employee based on their role
        private async Task CreateLeaveBalancesForEmployee(int employeeId, int employeeRoleConfigId, int campusId, string? createdBy)
        {
            // Use a consistent timestamp for all operations
            var currentDateTime = DateTime.Now;
            var currentYear = currentDateTime.Year;
            var currentMonth = currentDateTime.Month;

            // Get the role config
            var roleConfig = await _context.EmployeeRoleConfigs
                .FirstOrDefaultAsync(rc => rc.Id == employeeRoleConfigId);

            if (roleConfig == null)
                return;

            // Find all active leave configs for this role
            var leaveConfigs = await _context.LeaveConfigs
                .Where(lc => lc.IsActive &&
                            lc.RoleName == roleConfig.RoleName &&
                            lc.EmployeeType == roleConfig.EmployeeType &&
                            lc.CampusId == campusId)
                .ToListAsync();

            if (!leaveConfigs.Any())
                return;

            // Load all existing balances for this employee in a single query to avoid N+1
            var existingBalances = await _context.LeaveBalances
                .Where(lb =>
                    lb.EmployeeId == employeeId &&
                    lb.Year == currentYear &&
                    lb.CampusId == campusId)
                .ToListAsync();

            foreach (var config in leaveConfigs)
            {
                // Check if leave balance already exists
                var existingBalance = existingBalances.FirstOrDefault(lb =>
                    lb.LeaveType == config.LeaveType &&
                    (config.AllocationPeriod == "Yearly" ? lb.Month == null : lb.Month == currentMonth));

                if (existingBalance == null)
                {
                    var leaveBalance = new LeaveBalance
                    {
                        EmployeeId = employeeId,
                        LeaveType = config.LeaveType,
                        Year = currentYear,
                        Month = config.AllocationPeriod == "Monthly" ? currentMonth : (int?)null,
                        TotalAllocated = config.AllowedDays,
                        Used = 0,
                        CarriedForward = 0,
                        CreatedBy = createdBy ?? "System",
                        CreatedAt = currentDateTime,
                        CampusId = campusId
                    };

                    _context.LeaveBalances.Add(leaveBalance);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}