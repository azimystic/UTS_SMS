using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UTS_SMS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Controllers
{
    public class StudentPromotionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentPromotionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentPromotion
        public async Task<IActionResult> Index()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            IQueryable<Campus> campusQuery = _context.Campuses.Where(c => c.IsActive);

            // Filter by campus for non-owner users
            if (userCampusId.HasValue && userCampusId.Value != 0)
            {
                campusQuery = campusQuery.Where(c => c.Id == userCampusId.Value);
            }

            var campuses = await campusQuery
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Campuses = campuses;
            return View();
        }

        // GET: Classes by Campus
        [HttpGet]
        public async Task<IActionResult> GetClassesByCampus(int campusId)
        {
            var classes = await _context.Classes
                .Where(c => c.CampusId == campusId && c.IsActive)
                .OrderBy(c => c.GradeLevel)
                .Select(c => new { Id = c.Id, Name = c.Name, GradeLevel = c.GradeLevel })
                .ToListAsync();

            return Json(classes);
        }

        // GET: Sections by Class
        [HttpGet]
        public async Task<IActionResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(s => s.ClassId == classId && s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new { Id = s.Id, Name = s.Name })
                .ToListAsync();

            return Json(sections);
        }

        // GET: Target Classes (Higher grades only)
        [HttpGet]
        public async Task<IActionResult> GetTargetClasses(int campusId, string currentGradeLevel)
        {
            // Parse current grade level (assuming it's numeric like "1", "2", ..., "12")
            if (!int.TryParse(currentGradeLevel, out int currentGrade))
            {
                return Json(new List<object>());
            }

            var targetClasses = new List<object>();

            // Add higher grade classes
            var higherClasses = await _context.Classes
                .Where(c => c.CampusId == campusId && c.IsActive)
                .OrderBy(c => c.GradeLevel)
                .ToListAsync();

            foreach (var cls in higherClasses)
            {
                if (int.TryParse(cls.GradeLevel, out int grade) && grade > currentGrade)
                {
                    targetClasses.Add(new { Id = cls.Id, Name = cls.Name, GradeLevel = cls.GradeLevel });
                }
            }

            // If current grade is 12, add PassOut option
            if (currentGrade == 12)
            {
                targetClasses.Add(new { Id = -1, Name = "PassOut", GradeLevel = "PassOut" });
            }

            return Json(targetClasses);
        }

        // GET: Get Current Class Grade Level
        [HttpGet]
        public async Task<IActionResult> GetClassGradeLevel(int classId)
        {
            var classObj = await _context.Classes
                .Where(c => c.Id == classId)
                .Select(c => new { c.GradeLevel })
                .FirstOrDefaultAsync();

            return Json(classObj?.GradeLevel ?? "");
        }

        // GET: Students by Class and Section
        [HttpGet]
        public async Task<IActionResult> GetStudentsByClassSection(int classId, int sectionId)
        {
            var students = await _context.Students
                .Where(s => s.Class == classId && s.Section == sectionId && !s.HasLeft)
                .Select(s => new
                {
                    Id = s.Id,
                    StudentName = s.StudentName,
                    FatherName = s.FatherName,
                    StudentCNIC = s.StudentCNIC,
                    SubjectsGroupingId = s.SubjectsGroupingId
                })
                .OrderBy(s => s.StudentName)
                .ToListAsync();

            return Json(students);
        }

        // POST: Promote Students
        [HttpPost]
        public async Task<IActionResult> PromoteStudents([FromBody] PromotionRequestDto request)
        {
            try
            {
                if (request.StudentIds == null || !request.StudentIds.Any())
                {
                    return Json(new { success = false, message = "No students selected for promotion." });
                }

                var students = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToListAsync();

                if (!students.Any())
                {
                    return Json(new { success = false, message = "No students found." });
                }

                var campusId = students.First().CampusId;
                var fromClass = students.First().ClassObj;
                var fromSection = students.First().SectionObj;

                // Get target class and section names for notifications
                string toClassName = "";
                string toSectionName = "";

                if (!request.IsPassOut && request.ToClassId.HasValue && request.ToSectionId.HasValue)
                {
                    var toClass = await _context.Classes.FindAsync(request.ToClassId.Value);
                    var toSection = await _context.ClassSections.FindAsync(request.ToSectionId.Value);
                    toClassName = toClass?.Name ?? "";
                    toSectionName = toSection?.Name ?? "";
                }

                if (request.IsPassOut)
                {
                    // Mark students as left (PassOut)
                    foreach (var student in students)
                    {
                        student.HasLeft = true;
                    }
                }
                else
                {
                    // Promote to new class, section, and subject grouping
                    foreach (var student in students)
                    {
                        student.Class = request.ToClassId.Value;
                        student.Section = request.ToSectionId.Value;
                        
                        // Update subject grouping from per-student mapping or fall back to ToSubjectsGroupingId
                        if (request.StudentSubjectGroupings != null && 
                            request.StudentSubjectGroupings.TryGetValue(student.Id, out var studentGroupingId))
                        {
                            // Only update if groupingId > 0 (0 means "Keep Same")
                            if (studentGroupingId > 0)
                            {
                                student.SubjectsGroupingId = studentGroupingId;
                            }
                            // If studentGroupingId is 0, keep the existing SubjectsGroupingId
                        }
                        else if (request.ToSubjectsGroupingId.HasValue && request.ToSubjectsGroupingId.Value > 0)
                        {
                            student.SubjectsGroupingId = request.ToSubjectsGroupingId.Value;
                        }
                        // Otherwise, keep existing subject grouping
                        
                        // Handle student category updates during promotion
                        if (request.UpdateStudentCategory && request.NewStudentCategoryId.HasValue)
                        {
                            student.StudentCategoryId = request.NewStudentCategoryId;
                            
                            // Update or create category assignment
                            var existingAssignment = await _context.StudentCategoryAssignments
                                .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.IsActive);

                            if (existingAssignment != null)
                            {
                                existingAssignment.StudentCategoryId = request.NewStudentCategoryId.Value;
                                existingAssignment.ModifiedDate = DateTime.Now;
                                existingAssignment.ModifiedBy = User.Identity.Name;
                            }
                            else if (request.NewStudentCategoryId.Value > 0)
                            {
                                var category = await _context.StudentCategories
                                    .FirstOrDefaultAsync(c => c.Id == request.NewStudentCategoryId.Value);
                                
                                if (category != null)
                                {
                                    var assignment = new StudentCategoryAssignment
                                    {
                                        StudentId = student.Id,
                                        StudentCategoryId = category.Id,
                                        AppliedAdmissionFeeDiscount = student.AdmissionFeeDiscountAmount ?? 0,
                                        AppliedTuitionFeeDiscount = student.TuitionFeeDiscountPercent ?? 0,
                                        AssignedDate = DateTime.Now,
                                        AssignedBy = User.Identity.Name,
                                        IsActive = true
                                    };
                                    _context.StudentCategoryAssignments.Add(assignment);
                                }
                            }
                        }
                    }
                    
                    // Assign selected class charges for the new class
                    var chargesToAssign = new List<ClassFeeExtraCharges>();
                    if (request.SelectedClassCharges != null && request.SelectedClassCharges.Any())
                    {
                        // Use only the charges selected by the user
                        chargesToAssign = await _context.ClassFeeExtraCharges
                            .Where(c => request.SelectedClassCharges.Contains(c.Id) && c.IsActive && !c.IsDeleted)
                            .ToListAsync();
                    }
                    else
                    {
                        // Fallback to all class charges if none were explicitly selected
                        chargesToAssign = await _context.ClassFeeExtraCharges
                            .Where(c => c.ClassId == request.ToClassId.Value && c.IsActive && !c.IsDeleted)
                            .ToListAsync();
                    }
                    
                    foreach (var student in students)
                    {
                        foreach (var charge in chargesToAssign)
                        {
                            // Check if assignment already exists
                            var existingChargeAssignment = await _context.StudentChargeAssignments
                                .FirstOrDefaultAsync(sca => sca.StudentId == student.Id && 
                                                          sca.ClassFeeExtraChargeId == charge.Id);
                            
                            if (existingChargeAssignment == null)
                            {
                                // Create new charge assignment
                                var chargeAssignment = new StudentChargeAssignment
                                {
                                    StudentId = student.Id,
                                    ClassFeeExtraChargeId = charge.Id,
                                    IsAssigned = true,
                                    AssignedDate = DateTime.Now,
                                    AssignedBy = User.Identity.Name,
                                    CampusId = student.CampusId
                                };
                                _context.StudentChargeAssignments.Add(chargeAssignment);
                            }
                            else if (!existingChargeAssignment.IsAssigned)
                            {
                                // Re-activate existing assignment
                                existingChargeAssignment.IsAssigned = true;
                                existingChargeAssignment.ModifiedDate = DateTime.Now;
                                existingChargeAssignment.ModifiedBy = User.Identity.Name;
                            }
                        }
                    }
                    
                    // Save changes first so we can regenerate roll numbers
                    await _context.SaveChangesAsync();
                    
                    // Fetch campus once for all students (they're all from same campus)
                    var campus = await _context.Campuses.FindAsync(campusId);
                    
                    // Regenerate roll numbers for promoted students since their class/section changed
                    foreach (var student in students)
                    {
                        student.RollNumber = await GenerateRollNumber(student, campus);
                    }
                }

                await _context.SaveChangesAsync();

                // Create notifications for admin
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var campusAdmins = adminUsers.Where(u => u.CampusId == campusId).ToList();
                
                if (!campusAdmins.Any())
                {
                    campusAdmins = adminUsers.ToList(); // Fallback to all admins
                }

                foreach (var admin in campusAdmins)
                {
                    var adminNotification = new Notification
                    {
                        Type = "promotion",
                        Title = request.IsPassOut ? "Students Marked as PassOut" : "Students Promoted",
                        Message = request.IsPassOut 
                            ? $"{students.Count} student(s) from {fromClass?.Name ?? "Unknown"}-{fromSection?.Name ?? "Unknown"} have been marked as PassOut."
                            : $"{students.Count} student(s) have been promoted from {fromClass?.Name ?? "Unknown"}-{fromSection?.Name ?? "Unknown"} to {toClassName}-{toSectionName}.",
                        Timestamp = DateTime.Now,
                        UserId = admin.Id,
                        TargetRole = "Admin",
                        ActionUrl = "/Students/Index",
                        CampusId = campusId,
                        CreatedBy = User.Identity?.Name,
                        IsActive = true
                    };
                    _context.Notifications.Add(adminNotification);
                }

                // Create notifications for each promoted student
                if (!request.IsPassOut)
                {
                    foreach (var student in students)
                    {
                        var studentNotification = new Notification
                        {
                            Type = "promotion",
                            Title = "You Have Been Promoted!",
                            Message = $"Congratulations! You have been promoted to {toClassName}-{toSectionName}.",
                            Timestamp = DateTime.Now,
                            TargetRole = "Student",
                            RelatedEntityId = student.Id,
                            RelatedEntityType = "Student",
                            ActionUrl = "/StudentDashboard/Dashboard",
                            CampusId = campusId,
                            CreatedBy = User.Identity?.Name,
                            IsActive = true
                        };
                        _context.Notifications.Add(studentNotification);
                    }
                }

                await _context.SaveChangesAsync();

                var message = request.IsPassOut
                    ? $"{students.Count} students have been marked as PassOut successfully."
                    : $"{students.Count} students have been promoted successfully.";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // GET: Subject Groupings by Campus
        [HttpGet]
        public async Task<IActionResult> GetSubjectsGroupingsByCampus(int campusId)
        {
            var subjectsGroupings = await _context.SubjectsGroupings
                .Where(sg => sg.CampusId == campusId && sg.IsActive)
                .OrderBy(sg => sg.Name)
                .Select(sg => new { Id = sg.Id, Name = sg.Name })
                .ToListAsync();

            return Json(subjectsGroupings);
        }

        // GET: Student Categories by Campus
        [HttpGet]
        public async Task<IActionResult> GetStudentCategoriesByCampus(int campusId)
        {
            var categories = await _context.StudentCategories
                .Where(c => c.CampusId == campusId && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.CategoryName,
                    Type = c.CategoryType,
                    DefaultAdmissionDiscount = c.DefaultAdmissionFeeDiscount,
                    DefaultTuitionDiscount = c.DefaultTuitionFeeDiscount
                })
                .ToListAsync();

            return Json(categories);
        }
        
        // GET: Class Charges by Class
        [HttpGet]
        public async Task<IActionResult> GetClassChargesByClass(int classId)
        {
            var charges = await _context.ClassFeeExtraCharges
                .Where(c => c.ClassId == classId && c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.ChargeName)
                .Select(c => new
                {
                    Id = c.Id,
                    ChargeName = c.ChargeName,
                    Amount = c.Amount,
                    Category = c.Category
                })
                .ToListAsync();

            return Json(charges);
        }
        
        // Helper method to generate roll number for a student
        private async Task<string> GenerateRollNumber(Student student, Campus campus = null)
        {
            // Use provided campus or fetch if not provided
            if (campus == null)
            {
                campus = await _context.Campuses.FindAsync(student.CampusId);
            }
            
            var campusCode = campus?.Code ?? "UNK";
            var yearOfAdmission = student.RegistrationDate.Year;
            
            // Get the grade level from the class
            var classObj = await _context.Classes.FindAsync(student.Class);
            var gradeLevel = classObj?.GradeLevel ?? "UNK";

            // Get the count of students in the same class and section with the same admission year
            var existingCount = await _context.Students
                .Where(s => s.CampusId == student.CampusId 
                    && s.Class == student.Class 
                    && s.Section == student.Section
                    && s.RegistrationDate.Year == yearOfAdmission
                    && s.Id != student.Id)  // Exclude current student when updating
                .CountAsync();

            var incrementalNumber = (existingCount + 1).ToString("D3");
            return $"{campusCode}-{yearOfAdmission}-G{gradeLevel}-{incrementalNumber}";
        }
    }

    // DTO classes
    public class PromotionRequestDto
    {
        public int CampusId { get; set; }
        public int FromClassId { get; set; }
        public int FromSectionId { get; set; }
        public int? ToClassId { get; set; }
        public int? ToSectionId { get; set; }
        public int? ToSubjectsGroupingId { get; set; }
        public Dictionary<int, int> StudentSubjectGroupings { get; set; } = new Dictionary<int, int>();
        public List<int> StudentIds { get; set; } = new List<int>();
        public List<int> SelectedClassCharges { get; set; } = new List<int>();
        public bool IsPassOut { get; set; }
        public bool UpdateStudentCategory { get; set; }
        public int? NewStudentCategoryId { get; set; }
    }
}
