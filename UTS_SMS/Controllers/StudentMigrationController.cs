using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentMigrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public StudentMigrationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: StudentMigration/Index (List all migration requests)
        public async Task<IActionResult> Index(string status = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;
            
            var migrationsQuery = _context.StudentMigrations
                .Include(m => m.Student)
                    .ThenInclude(s => s.ClassObj)
                .Include(m => m.Student)
                    .ThenInclude(s => s.SectionObj)
                .Include(m => m.FromCampus)
                .Include(m => m.ToCampus)
                .Where(m => m.Status == status && m.IsActive);

            // Filter by campus: Show migrations where user's campus is involved (either from or to)
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                migrationsQuery = migrationsQuery.Where(m => 
                    m.ToCampusId == userCampusId.Value || m.FromCampusId == userCampusId.Value);
            }

            var migrations = await migrationsQuery
                .OrderByDescending(m => m.RequestedDate)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.UserCampusId = userCampusId;
            ViewBag.IsOwner = isOwner;
            
            // Filter campuses dropdown
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToListAsync();
            }
            else
            {
                ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            }
            
            return View(migrations);
        }

        // POST: Request migration from Student Index
        [HttpPost]
        public async Task<JsonResult> RequestMigration([FromBody] StudentMigrationRequestDto request)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Campus)
                    .FirstOrDefaultAsync(s => s.Id == request.StudentId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                // Check if student has outstanding dues
                var latestBilling = await _context.BillingMaster
                    .Where(b => b.StudentId == student.Id)
                    .OrderByDescending(b => b.CreatedDate)
                    .FirstOrDefaultAsync();

                decimal outstandingDues = latestBilling?.Dues ?? 0;

                if (outstandingDues > 0 && !request.ForceSubmit)
                {
                    return Json(new
                    {
                        success = false,
                        hasDues = true,
                        outstandingDues = outstandingDues,
                        message = $"Student has outstanding dues of {outstandingDues:N2}. Please clear dues before migration."
                    });
                }

                // Check if migration request already exists
                var existingRequest = await _context.StudentMigrations
                    .FirstOrDefaultAsync(m => m.StudentId == request.StudentId && 
                                            m.Status == "Pending" && 
                                            m.IsActive);

                if (existingRequest != null)
                {
                    return Json(new { success = false, message = "A pending migration request already exists for this student" });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                
                var migration = new StudentMigration
                {
                    StudentId = request.StudentId,
                    FromCampusId = student.CampusId,
                    ToCampusId = request.ToCampusId,
                    FromClassId = student.Class,
                    FromSectionId = student.Section,
                    Remarks = request.Remarks,
                    OutstandingDues = outstandingDues,
                    Status = "Pending",
                    RequestedDate = DateTime.Now,
                    RequestedBy = User.Identity.Name,
                    IsActive = true
                };

                _context.StudentMigrations.Add(migration);
                await _context.SaveChangesAsync();

                // Create notification for ToCampus admins
                var toCampus = await _context.Campuses.FindAsync(request.ToCampusId);
                if (toCampus != null)
                {
                    var notification = new Notification
                    {
                        Type = "migration",
                        Title = "New Student Migration Request",
                        Message = $"Migration request received for student {student.StudentName} from {student.Campus.Name} to {toCampus.Name}.",
                        Timestamp = DateTime.Now,
                        TargetRole = "Admin",
                        RelatedEntityId = migration.Id,
                        RelatedEntityType = "StudentMigration",
                        ActionUrl = $"/StudentMigration/Index?status=Pending",
                        CampusId = request.ToCampusId,
                        CreatedBy = User.Identity.Name
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = "Migration request submitted successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Approve migration
        [HttpPost]
        public async Task<IActionResult> ApproveMigration(int id, int targetClassId, int targetSectionId, int? targetSubjectsGroupingId, int? targetStudentCategoryId)
        {
            try
            {
                if (targetClassId <= 0 || targetSectionId <= 0)
                {
                    TempData["ErrorMessage"] = "Please select both target class and section";
                    return RedirectToAction(nameof(Index));
                }

                var migration = await _context.StudentMigrations
                    .Include(m => m.Student)
                    .ThenInclude(s => s.Family)
                    .Include(m => m.FromCampus)
                    .Include(m => m.ToCampus)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (migration == null)
                {
                    TempData["ErrorMessage"] = "Migration request not found";
                    return RedirectToAction(nameof(Index));
                }

                // Validate campus access
                var currentUser = await _userManager.GetUserAsync(User);
                var userCampusId = currentUser?.CampusId;
                var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

                // Only ToCampus (or Owner) can approve migrations
                if (!isOwner)
                {
                    if (!userCampusId.HasValue || migration.ToCampusId != userCampusId.Value)
                    {
                        TempData["ErrorMessage"] = "Only the target campus can approve this migration";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (migration.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Only pending requests can be approved";
                    return RedirectToAction(nameof(Index));
                }

                // Verify class and section belong to target campus
                var targetClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == targetClassId && c.CampusId == migration.ToCampusId && c.IsActive);
                
                var targetSection = await _context.ClassSections
                    .FirstOrDefaultAsync(s => s.Id == targetSectionId && s.ClassId == targetClassId && s.IsActive);

                if (targetClass == null || targetSection == null)
                {
                    TempData["ErrorMessage"] = "Invalid class or section selection";
                    return RedirectToAction(nameof(Index));
                }

                // Mark student as left from current campus
                migration.Student.HasLeft = true;

                // Create a new student record for the new campus
                var newStudent = new Student
                {
                    StudentName = migration.Student.StudentName,
                    FatherName = migration.Student.FatherName,
                    StudentCNIC = migration.Student.StudentCNIC + "_MIGRATED_" + DateTime.Now.Ticks,
                    FatherCNIC = migration.Student.FatherCNIC,
                    Gender = migration.Student.Gender,
                    Class = targetClassId,
                    Section = targetSectionId,
                    CampusId = migration.ToCampusId,
                    SubjectsGroupingId = targetSubjectsGroupingId ?? migration.Student.SubjectsGroupingId,
                    PhoneNumber = migration.Student.PhoneNumber,
                    FatherPhone = migration.Student.FatherPhone,
                    HomeAddress = migration.Student.HomeAddress,
                    IsFatherDeceased = migration.Student.IsFatherDeceased,
                    GuardianName = migration.Student.GuardianName,
                    GuardianPhone = migration.Student.GuardianPhone,
                    DateOfBirth = migration.Student.DateOfBirth,
                    FatherSourceOfIncome = migration.Student.FatherSourceOfIncome,
                    PreviousSchool = migration.Student.PreviousSchool,
                    TuitionFeeDiscountPercent = migration.Student.TuitionFeeDiscountPercent,
                    AdmissionFeeDiscountAmount = migration.Student.AdmissionFeeDiscountAmount,
                    StudentCategoryId = targetStudentCategoryId ?? migration.Student.StudentCategoryId,
                    MatricRollNumber = migration.Student.MatricRollNumber,
                    InterRollNumber = migration.Student.InterRollNumber,
                    PersonalTitle = migration.Student.PersonalTitle,
                    Notification = migration.Student.Notification,
                    ProfilePicture = migration.Student.ProfilePicture,
                    FatherCNIC_Front = migration.Student.FatherCNIC_Front,
                    FatherCNIC_Back = migration.Student.FatherCNIC_Back,
                    BForm = migration.Student.BForm,
                    StudentCNIC_Front = migration.Student.StudentCNIC_Front,
                    StudentCNIC_Back = migration.Student.StudentCNIC_Back,
                    MatricCertificate_01 = migration.Student.MatricCertificate_01,
                    InterCertificate_01 = migration.Student.InterCertificate_01,
                    MatricCertificate_02 = migration.Student.MatricCertificate_02,
                    InterCertificate_02 = migration.Student.InterCertificate_02,
                    AdmissionFeePaid = true,
                    HasLeft = false,
                    RegistrationDate = DateTime.Now,
                    RegisteredBy = $"MIGRATED from {migration.FromCampus.Name} by {User.Identity.Name}",
                    MotherName = migration.Student.MotherName,
                    MotherCNIC = migration.Student.MotherCNIC,
                    MotherPhone = migration.Student.MotherPhone
                };

                // Handle family migration
                if (migration.Student.FamilyId.HasValue)
                {
                    var family = migration.Student.Family;
                    
                    var existingFamily = await _context.Families
                        .FirstOrDefaultAsync(f => f.FatherCNIC == family.FatherCNIC && 
                                                f.CampusId == migration.ToCampusId && 
                                                f.IsActive);

                    if (existingFamily != null)
                    {
                        newStudent.FamilyId = existingFamily.Id;
                    }
                    else
                    {
                        var newFamily = new Family
                        {
                            FatherName = family.FatherName,
                            FatherCNIC = family.FatherCNIC,
                            FatherPhone = family.FatherPhone,
                            FatherSourceOfIncome = family.FatherSourceOfIncome,
                            MotherName = family.MotherName,
                            MotherCNIC = family.MotherCNIC,
                            MotherPhone = family.MotherPhone,
                            HomeAddress = family.HomeAddress,
                            CampusId = migration.ToCampusId,
                            IsFatherDeceased = family.IsFatherDeceased,
                            GuardianName = family.GuardianName,
                            GuardianPhone = family.GuardianPhone,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            CreatedBy = User.Identity.Name
                        };
                        
                        _context.Families.Add(newFamily);
                        await _context.SaveChangesAsync();
                        
                        newStudent.FamilyId = newFamily.Id;
                    }
                }

                _context.Students.Add(newStudent);

                // Update migration status
                migration.Status = "Approved";
                migration.ApprovedDate = DateTime.Now;
                migration.ApprovedBy = User.Identity.Name;
                migration.ProcessedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Create notification for FromCampus (requester campus) about approval
                var fromCampusNotification = new Notification
                {
                    Type = "migration",
                    Title = "Student Migration Approved",
                    Message = $"Migration approved for student {migration.Student.StudentName} from {migration.FromCampus.Name} to {migration.ToCampus.Name}.",
                    Timestamp = DateTime.Now,
                    TargetRole = "Admin",
                    RelatedEntityId = migration.Id,
                    RelatedEntityType = "StudentMigration",
                    ActionUrl = $"/StudentMigration/Index?status=Approved",
                    CampusId = migration.FromCampusId,
                    CreatedBy = User.Identity.Name
                };
                _context.Notifications.Add(fromCampusNotification);
                
                // Create notification for ToCampus about successful migration
                var toCampusNotification = new Notification
                {
                    Type = "migration",
                    Title = "Student Successfully Migrated",
                    Message = $"Student {migration.Student.StudentName} has been successfully migrated from {migration.FromCampus.Name}.",
                    Timestamp = DateTime.Now,
                    TargetRole = "Admin",
                    RelatedEntityId = migration.Id,
                    RelatedEntityType = "StudentMigration",
                    ActionUrl = $"/Students/Edit/{newStudent.Id}",
                    CampusId = migration.ToCampusId,
                    CreatedBy = User.Identity.Name
                };
                _context.Notifications.Add(toCampusNotification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Migration approved successfully. Student '{migration.Student.StudentName}' migrated from {migration.FromCampus.Name} to {migration.ToCampus.Name} ({targetClass.Name} - {targetSection.Name})";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error approving migration: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Reject migration
        [HttpPost]
        public async Task<IActionResult> RejectMigration(int id, string rejectionReason)
        {
            try
            {
                var migration = await _context.StudentMigrations
                    .Include(m => m.FromCampus)
                    .Include(m => m.ToCampus)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (migration == null)
                {
                    TempData["ErrorMessage"] = "Migration request not found";
                    return RedirectToAction(nameof(Index));
                }

                // Validate campus access
                var currentUser = await _userManager.GetUserAsync(User);
                var userCampusId = currentUser?.CampusId;
                var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

                // Only ToCampus (or Owner) can reject migrations
                if (!isOwner)
                {
                    if (!userCampusId.HasValue || migration.ToCampusId != userCampusId.Value)
                    {
                        TempData["ErrorMessage"] = "Only the target campus can reject this migration";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (migration.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Only pending requests can be rejected";
                    return RedirectToAction(nameof(Index));
                }

                migration.Status = "Rejected";
                migration.RejectionReason = rejectionReason;
                migration.ProcessedDate = DateTime.Now;
                migration.ApprovedBy = User.Identity.Name;

                await _context.SaveChangesAsync();

                // Create notification for FromCampus (requester campus) about rejection
                // 1. Fetch the student name using the StudentId from the migration object
                var studentName = await _context.Students
                    .Where(s => s.Id == migration.StudentId)
                    .Select(s => s.StudentName)
                    .FirstOrDefaultAsync() ?? "Unknown Student";

                // 2. Create the notification using the retrieved name
                var notification = new Notification
                {
                    Type = "migration",
                    Title = "Student Migration Rejected",
                    // Use the studentName variable we just fetched
                    Message = $"Migration rejected for student {studentName} from {migration.FromCampus.Name} to {migration.ToCampus.Name}. Reason: {rejectionReason}",
                    Timestamp = DateTime.Now,
                    TargetRole = "Admin",
                    RelatedEntityId = migration.Id,
                    RelatedEntityType = "StudentMigration",
                    ActionUrl = "/StudentMigration/Index?status=Rejected",
                    CampusId = migration.FromCampusId,
                    CreatedBy = User.Identity.Name
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Migration request rejected successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error rejecting migration: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClassesByCampus(int campusId)
        {
            // Validate campus access
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Non-owners can only access their own campus data
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0 && campusId != userCampusId.Value)
            {
                return Json(new List<object>()); // Return empty list if unauthorized
            }

            var classes = await _context.Classes
                .Where(c => c.CampusId == campusId && c.IsActive)
                .OrderBy(c => c.GradeLevel)
                .Select(c => new { Id = c.Id, Name = c.Name })
                .ToListAsync();

            return Json(classes);
        }

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

        [HttpGet]
        public async Task<IActionResult> GetSubjectsGroupingsByCampus(int campusId)
        {
            // Validate campus access
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Non-owners can only access their own campus data
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0 && campusId != userCampusId.Value)
            {
                return Json(new List<object>()); // Return empty list if unauthorized
            }

            var groupings = await _context.SubjectsGroupings
                .Where(sg => sg.CampusId == campusId && sg.IsActive)
                .OrderBy(sg => sg.Name)
                .Select(sg => new { Id = sg.Id, Name = sg.Name })
                .ToListAsync();

            return Json(groupings);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentCategoriesByCampus(int campusId)
        {
            // Validate campus access
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Non-owners can only access their own campus data
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0 && campusId != userCampusId.Value)
            {
                return Json(new List<object>()); // Return empty list if unauthorized
            }

            var categories = await _context.StudentCategories
                .Where(sc => sc.CampusId == campusId && sc.IsActive)
                .OrderBy(sc => sc.CategoryName)
                .Select(sc => new { Id = sc.Id, Name = sc.CategoryName })
                .ToListAsync();

            return Json(categories);
        }
    }

    // DTOs
    public class StudentMigrationRequestDto
    {
        public int StudentId { get; set; }
        public int ToCampusId { get; set; }
        public string? Remarks { get; set; }
        public bool ForceSubmit { get; set; } = false;
    }
}
