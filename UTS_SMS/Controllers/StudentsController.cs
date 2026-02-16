using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
        }

        // GET: Students
        public async Task<IActionResult> Index(
            string sortOrder,
            string currentFilter,
            string searchString,
            string classFilter,
            string sectionFilter,
            string campusFilter,
            bool? showLeft,
            int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["ClassSortParm"] = sortOrder == "Class" ? "class_desc" : "Class";
            ViewData["FatherSortParm"] = sortOrder == "Father" ? "father_desc" : "Father";

            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;
            var isOwner = User.IsInRole("Owner");
            
            ViewBag.UserCampusId = userCampusId;
            ViewBag.IsOwner = isOwner;
            ViewBag.CampusesAll = await _context.Campuses
                    .Where(c => c.IsActive)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();
            // Dropdowns for filters
            if (userCampusId == null || userCampusId == 0)
            {
                ViewBag.Classes = await _context.Students
                    .Select(s => s.ClassObj.Name)
                    .Distinct().OrderBy(c => c).ToListAsync();

                ViewBag.Sections = await _context.Students
                    .Select(s => s.SectionObj.Name)
                    .Distinct().OrderBy(s => s).ToListAsync();

                ViewBag.Campuses = await _context.Campuses
                    .Where(c => c.IsActive)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();
            }
            else
            {
                ViewBag.Classes = await _context.Students
                    .Where(s => s.CampusId == userCampusId)
                    .Select(s => s.ClassObj.Name)
                    .Distinct().OrderBy(c => c).ToListAsync();

                ViewBag.Sections = await _context.Students
                    .Where(s => s.CampusId == userCampusId)
                    .Select(s => s.SectionObj.Name)
                    .Distinct().OrderBy(s => s).ToListAsync();

                ViewBag.Campuses = await _context.Campuses
                    .Where(c => c.IsActive && c.Id == userCampusId)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();
            }

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            // Preserve filter values
            ViewData["CurrentFilter"] = searchString;
            ViewData["ClassFilter"] = classFilter;
            ViewData["SectionFilter"] = sectionFilter;
            ViewData["CampusFilter"] = campusFilter;
            ViewData["ShowLeft"] = showLeft;

            // Main query
            var students = _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .AsQueryable();

            // Restrict to user campus if not admin
            if (userCampusId != null && userCampusId != 0)
            {
                students = students.Where(s => s.CampusId == userCampusId);
            }

            // Apply filters
            if (showLeft.HasValue)
                students = students.Where(s => s.HasLeft == showLeft);

            if (!string.IsNullOrEmpty(searchString))
                students = students.Where(s =>
                    s.StudentName.Contains(searchString) ||
                    s.FatherName.Contains(searchString) ||
                    s.StudentCNIC.Contains(searchString) ||
                    s.FatherCNIC.Contains(searchString) ||
                    s.PhoneNumber.Contains(searchString) ||
                    s.FatherPhone.Contains(searchString));

            if (!string.IsNullOrEmpty(classFilter))
                students = students.Where(s => s.ClassObj.Name == classFilter);

            if (!string.IsNullOrEmpty(sectionFilter))
                students = students.Where(s => s.SectionObj.Name == sectionFilter);

            if (!string.IsNullOrEmpty(campusFilter))
                students = students.Where(s => s.Campus.Name == campusFilter);

            // Sorting
            switch (sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.StudentName);
                    break;
                case "Father":
                    students = students.OrderBy(s => s.FatherName);
                    break;
                case "father_desc":
                    students = students.OrderByDescending(s => s.FatherName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.RegistrationDate);
                    break;
                case "date_desc":
                    students = students.OrderByDescending(s => s.RegistrationDate);
                    break;
                case "Class":
                    students = students.OrderBy(s => s.ClassObj.Name);
                    break;
                case "class_desc":
                    students = students.OrderByDescending(s => s.ClassObj.Name);
                    break;
                default:
                    students = students.OrderBy(s => s.StudentName);
                    break;
            }

            int pageSize = 10;
            return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(), pageNumber ?? 1, pageSize));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsLeft(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            student.HasLeft = true;
            student.LeftDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Log admin notification for marking student as left
            await LogAdminNotification(
                action: "Student Marked as Left",
                description: $"Student '{student.StudentName}' (Roll No: {student.RollNumber}) was marked as left by {User.Identity.Name}",
                studentId: student.Id,
                campusId: student.CampusId,
                entityType: "Student",
                entityId: student.Id
            );

            TempData["SuccessMessage"] = "Student marked as left successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsActive(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            student.HasLeft = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student marked as active successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Create
        public async Task<IActionResult> Create(int? migrationId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            var isOwner = User.IsInRole("Owner");
            
            // Pass user info to view for campus dropdown logic
            ViewBag.UserCampusId = campusId;
            ViewBag.IsOwner = isOwner;
            
            Student student = null;
            
            // If migrationId is provided, load the student data from migration
            if (migrationId.HasValue)
            {
                var migration = await _context.StudentMigrations
                    .Include(m => m.Student)
                    .FirstOrDefaultAsync(m => m.Id == migrationId.Value);
                
                if (migration != null && migration.Status == "Pending")
                {
                    // Pre-fill student data from migration (excluding Class, Section, SubjectGrouping, StudentCategory)
                    student = new Student
                    {
                        StudentName = migration.Student.StudentName,
                        FatherName = migration.Student.FatherName,
                        StudentCNIC = migration.Student.StudentCNIC,
                        FatherCNIC = migration.Student.FatherCNIC,
                        Gender = migration.Student.Gender,
                        PhoneNumber = migration.Student.PhoneNumber,
                        FatherPhone = migration.Student.FatherPhone,
                        HomeAddress = migration.Student.HomeAddress,
                        IsFatherDeceased = migration.Student.IsFatherDeceased,
                        GuardianName = migration.Student.GuardianName,
                        GuardianPhone = migration.Student.GuardianPhone,
                        DateOfBirth = migration.Student.DateOfBirth,
                        FatherSourceOfIncome = migration.Student.FatherSourceOfIncome,
                        PreviousSchool = migration.Student.PreviousSchool,
                        MatricRollNumber = migration.Student.MatricRollNumber,
                        InterRollNumber = migration.Student.InterRollNumber,
                        PersonalTitle = migration.Student.PersonalTitle,
                        MotherName = migration.Student.MotherName,
                        MotherCNIC = migration.Student.MotherCNIC,
                        MotherPhone = migration.Student.MotherPhone,
                        CampusId = migration.ToCampusId
                    };
                    
                    // Store migration ID and old student data in ViewBag for later use
                    ViewBag.MigrationId = migrationId.Value;
                    ViewBag.OldStudentId = migration.StudentId;
                    ViewBag.IsMigration = true;
                    campusId = migration.ToCampusId;
                }
            }
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name");
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name", campusId);
                ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name");
            } 
            else
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive  ), "Id", "Name");
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive ), "Id", "Name");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive ), "Id", "Name");
                ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive ), "Id", "Name");
            }
            
        
            
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile? profilePicture,
            IFormFile? fatherCnicFront, IFormFile? fatherCnicBack, IFormFile? bForm,
            IFormFile? studentCnicFront, IFormFile? studentCnicBack, IFormFile? matricCert1,
            IFormFile? interCert1, IFormFile? matricCert2, IFormFile? interCert2,
            List<int>? OptionalCharges,
            bool AdmissionInNextMonth = false,
            int? SelectedEmployeeId = null,
            int? CalculatedSiblingCount = null,
            string? PaymentMode = null,
            decimal? CustomAdmissionPercent = null,
            decimal? CustomTuitionPercent = null,
            decimal? FatherSalaryPercent = null,
            decimal? MotherSalaryPercent = null,
            int? MotherEmployeeId = null,
            int? MigrationId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            ModelState.Remove(nameof(Student.RegisteredBy));
            ModelState.Remove(nameof(Student.ClassObj));
            ModelState.Remove(nameof(Student.SectionObj));
            ModelState.Remove(nameof(Student.SubjectsGrouping));
            ModelState.Remove(nameof(Student.Campus));
            ModelState.Remove(nameof(Student.Family));
            ModelState.Remove(nameof(Student.StudentCategory));
            ModelState.Remove(nameof(Student.Email));

            // Remove image fields from ModelState validation
            ModelState.Remove("ProfilePicture");
            ModelState.Remove("profilePicture");
            ModelState.Remove("fatherCNICFront");
            ModelState.Remove("fatherCNICBack");
            ModelState.Remove("studentCNICFront");
            ModelState.Remove("studentCNICBack");
            ModelState.Remove("bForm");
            ModelState.Remove("matricCert1");
            ModelState.Remove("matricCert2");
            ModelState.Remove("interCert1");
            ModelState.Remove("interCert2");
            ModelState.Remove("FatherCNIC_Front");
            ModelState.Remove("FatherCNIC_Back");
            ModelState.Remove("StudentCNIC_Front");
            ModelState.Remove("StudentCNIC_Back");
            ModelState.Remove("BForm");
            ModelState.Remove("MatricCertificate_01");
            ModelState.Remove("MatricCertificate_02");
            ModelState.Remove("InterCertificate_01");
            ModelState.Remove("InterCertificate_02");

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle family creation/lookup
                    var family = await _context.Families
                        .FirstOrDefaultAsync(f => f.FatherCNIC == student.FatherCNIC);

                    if (family == null)
                    {
                        family = new Family
                        {
                            FatherName = student.FatherName,
                            FatherCNIC = student.FatherCNIC,
                            FatherPhone = student.FatherPhone,
                            FatherSourceOfIncome = student.FatherSourceOfIncome,
                            MotherName = student.MotherName,
                            MotherCNIC = student.MotherCNIC,
                            MotherPhone = student.MotherPhone,
                            HomeAddress = student.HomeAddress,
                            CampusId = student.CampusId
                        };
                        _context.Families.Add(family);
                        await _context.SaveChangesAsync();
                    }

                    student.FamilyId = family.Id;

                    // Handle migration - copy images from old student if migration exists
                    StudentMigration migration = null;
                    if (MigrationId.HasValue)
                    {
                        migration = await _context.StudentMigrations
                            .Include(m => m.Student)
                            .FirstOrDefaultAsync(m => m.Id == MigrationId.Value && m.Status == "Pending");
                        
                        if (migration != null)
                        {
                            var oldStudent = migration.Student;
                            
                            // Copy image paths from old student (no file upload needed, just reference)
                            if (profilePicture == null && !string.IsNullOrEmpty(oldStudent.ProfilePicture))
                                student.ProfilePicture = oldStudent.ProfilePicture;
                            
                            if (fatherCnicFront == null && !string.IsNullOrEmpty(oldStudent.FatherCNIC_Front))
                                student.FatherCNIC_Front = oldStudent.FatherCNIC_Front;
                            
                            if (fatherCnicBack == null && !string.IsNullOrEmpty(oldStudent.FatherCNIC_Back))
                                student.FatherCNIC_Back = oldStudent.FatherCNIC_Back;
                            
                            if (studentCnicFront == null && !string.IsNullOrEmpty(oldStudent.StudentCNIC_Front))
                                student.StudentCNIC_Front = oldStudent.StudentCNIC_Front;
                            
                            if (studentCnicBack == null && !string.IsNullOrEmpty(oldStudent.StudentCNIC_Back))
                                student.StudentCNIC_Back = oldStudent.StudentCNIC_Back;
                            
                            if (bForm == null && !string.IsNullOrEmpty(oldStudent.BForm))
                                student.BForm = oldStudent.BForm;
                            
                            if (matricCert1 == null && !string.IsNullOrEmpty(oldStudent.MatricCertificate_01))
                                student.MatricCertificate_01 = oldStudent.MatricCertificate_01;
                            
                            if (matricCert2 == null && !string.IsNullOrEmpty(oldStudent.MatricCertificate_02))
                                student.MatricCertificate_02 = oldStudent.MatricCertificate_02;
                            
                            if (interCert1 == null && !string.IsNullOrEmpty(oldStudent.InterCertificate_01))
                                student.InterCertificate_01 = oldStudent.InterCertificate_01;
                            
                            if (interCert2 == null && !string.IsNullOrEmpty(oldStudent.InterCertificate_02))
                                student.InterCertificate_02 = oldStudent.InterCertificate_02;
                        }
                    }

                    // Upload files (only if new files were provided)
                    if (profilePicture != null)
                        student.ProfilePicture = await student.UploadFile(profilePicture, "profile-pictures", _env);
                    if (fatherCnicFront != null)
                        student.FatherCNIC_Front = await student.UploadFile(fatherCnicFront, "cnic-documents", _env);
                    if (fatherCnicBack != null)
                        student.FatherCNIC_Back = await student.UploadFile(fatherCnicBack, "cnic-documents", _env);
                    if (studentCnicFront != null)
                        student.StudentCNIC_Front = await student.UploadFile(studentCnicFront, "cnic-documents", _env);
                    if (studentCnicBack != null)
                        student.StudentCNIC_Back = await student.UploadFile(studentCnicBack, "cnic-documents", _env);
                    if (bForm != null)
                        student.BForm = await student.UploadFile(bForm, "bform-documents", _env);
                    if (matricCert1 != null)
                        student.MatricCertificate_01 = await student.UploadFile(matricCert1, "certificates", _env);
                    if (matricCert2 != null)
                        student.MatricCertificate_02 = await student.UploadFile(matricCert2, "certificates", _env);
                    if (interCert1 != null)
                        student.InterCertificate_01 = await student.UploadFile(interCert1, "certificates", _env);
                    if (interCert2 != null)
                        student.InterCertificate_02 = await student.UploadFile(interCert2, "certificates", _env);

                    student.RegisteredBy = User.Identity.Name;
                    student.RegistrationDate = DateTime.Now;
                    student.AdmissionFeePaid = false;
                    student.HasLeft = false;
                    if (AdmissionInNextMonth)
                    {
                        DateTime today = DateTime.Today;
                        DateTime nextMonth = today.AddMonths(1);

                        student.RegistrationDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
                    }
                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    // Generate and assign roll number
                    student.RollNumber = await GenerateRollNumber(student);
                    _context.Update(student);
                    await _context.SaveChangesAsync();

                    // Log admin notification for student creation
                    await LogAdminNotification(
                        action: "Student Created",
                        description: $"New student '{student.StudentName}' (Roll No: {student.RollNumber}) was registered by {User.Identity.Name}",
                        studentId: student.Id,
                        campusId: student.CampusId,
                        entityType: "Student",
                        entityId: student.Id
                    );

                    // Handle StudentCategory assignment if category was selected
                    if (student.StudentCategoryId.HasValue)
                    {
                        var category = await _context.StudentCategories
                            .Include(c => c.EmployeeCategoryDiscounts)
                            .FirstOrDefaultAsync(c => c.Id == student.StudentCategoryId.Value);

                        if (category != null)
                        {
                            // Validation for Employee Parent category with salary deduction
                            if (category.CategoryType == "EmployeeParent" && PaymentMode == "DeductFromSalary")
                            {
                                if (!SelectedEmployeeId.HasValue)
                                {
                                    ModelState.AddModelError("", "Employee must be selected for Employee Parent category with salary deduction.");
                                }
                                else
                                {
                                    // Check if employee has a salary defined
                                    var employeeSalary = await _context.SalaryDefinitions
                                        .Where(sd => sd.EmployeeId == SelectedEmployeeId.Value && sd.IsActive)
                                        .FirstOrDefaultAsync();
                                    
                                    if (employeeSalary == null)
                                    {
                                        ModelState.AddModelError("", "Selected employee does not have a salary defined. Please define salary first.");
                                    }
                                    else
                                    {
                                        // Get class fee to check if salary is sufficient
                                        var classFee = await _context.ClassFees
                                            .FirstOrDefaultAsync(cf => cf.ClassId == student.Class);
                                        
                                        if (classFee != null)
                                        {
                                            var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                                            var deductionPercent = CustomTuitionPercent ?? 40m; // Default 40% if not specified
                                            var deductionAmount = (tuitionFee * deductionPercent) / 100m;
                                            
                                            // Check if net salary is sufficient for deduction
                                            if (employeeSalary.NetSalary < deductionAmount)
                                            {
                                                ModelState.AddModelError("", $"Employee's net salary ({employeeSalary.NetSalary:N2}) is insufficient to cover the fee deduction amount ({deductionAmount:N2}).");
                                            }
                                        }
                                    }
                                }
                            }
                            
                            // Only proceed if validation passed
                            if (!ModelState.IsValid)
                            {
                                // Validation failed, need to return to view with errors
                                // Repopulate dropdowns
                                if (campusId.HasValue && campusId.Value > 0)
                                {
                                    ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.Class);
                                    ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.Section);
                                    ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name", student.CampusId);
                                    ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.SubjectsGroupingId);
                                }
                                else
                                {
                                    ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive), "Id", "Name", student.Class);
                                    ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive), "Id", "Name", student.Section);
                                    ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name", student.CampusId);
                                    ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive), "Id", "Name", student.SubjectsGroupingId);
                                }
                                return View(student);
                            }
                            
                            var assignment = new StudentCategoryAssignment
                            {
                                StudentId = student.Id,
                                StudentCategoryId = category.Id,
                                EmployeeId = SelectedEmployeeId,
                                PaymentMode = PaymentMode ?? "0",
                                CustomAdmissionPercent = CustomAdmissionPercent,
                                CustomTuitionPercent = CustomTuitionPercent,
                                AppliedAdmissionFeeDiscount = student.AdmissionFeeDiscountAmount ?? 0,
                                AppliedTuitionFeeDiscount = student.TuitionFeeDiscountPercent ?? 0,
                                AssignedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                AssignedBy = User.Identity.Name,
                                ModifiedBy = User.Identity.Name,
                                IsActive = true
                            };

                            _context.StudentCategoryAssignments.Add(assignment);
                            await _context.SaveChangesAsync();
                            if (OptionalCharges != null)
                            {
                                var existingAssignments = await _context.StudentChargeAssignments
                                    .Where(sca => sca.StudentId == student.Id)
                                    .ToListAsync();

                                // Get all optional charges for the class
                                var allClassCharges = await _context.ClassFeeExtraCharges
                                    .Where(c => c.ClassId == student.Class)
                                    .Select(c => c.Id)
                                    .ToListAsync();

                                foreach (var chargeId in allClassCharges)
                                {
                                    var existingAssignment = existingAssignments.FirstOrDefault(a => a.ClassFeeExtraChargeId == chargeId);
                                    var isAssigned = OptionalCharges.Contains(chargeId);

                                    if (existingAssignment != null)
                                    {
                                        existingAssignment.IsAssigned = isAssigned;
                                    }
                                    else
                                    {
                                        _context.StudentChargeAssignments.Add(new StudentChargeAssignment
                                        {
                                            StudentId = student.Id,
                                            ClassFeeExtraChargeId = chargeId,
                                            IsAssigned = isAssigned,
                                            AssignedBy = User.Identity.Name,
                                            AssignedDate = DateTime.Now,
                                            CampusId = student.CampusId
                                        });
                                    }
                                }

                                await _context.SaveChangesAsync();
                            }
                            // Update sibling discounts if Sibling category
                            // Update sibling discounts if Sibling category
                            if (category.CategoryType == "Sibling" && CalculatedSiblingCount.HasValue && CalculatedSiblingCount.Value > 0)
                            {
                                await UpdateSiblingDiscounts(
                                    student.FamilyId.Value,
                                    student.AdmissionFeeDiscountAmount ?? 0,
                                    student.TuitionFeeDiscountPercent ?? 0,
                                    category.Id  // ✅ ADD: Pass category ID
                                );
                            }
                        }
                    }

                    // Create user account
                    var result = await _userService.CreateStudentUserAsync(student);
                    if (result.Succeeded)
                    {
                        _context.Update(student);
                        await _context.SaveChangesAsync();

                        TempData["Success"] = $"Student and user account created successfully! Username: {student.StudentName.Replace(" ", "").ToLower()}, Password: student123";
                    }
                    else
                    {
                        TempData["Warning"] = "Student created but user account creation failed. Please create manually.";
                    }

                    // Approve migration if this was a migration create
                    if (migration != null)
                    {
                        // Mark old student as left
                        migration.Student.HasLeft = true;
                        _context.Update(migration.Student);
                        
                        // Update migration status
                        migration.Status = "Approved";
                        migration.ApprovedDate = DateTime.Now;
                        migration.ApprovedBy = User.Identity.Name;
                        migration.ProcessedDate = DateTime.Now;
                        _context.Update(migration);
                        
                        await _context.SaveChangesAsync();
                        
                        TempData["SuccessMessage"] = $"Migration approved successfully! Student '{student.StudentName}' has been migrated to the new campus.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Student registered successfully!";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            // Repopulate ViewBag on error
            if (campusId == null || campusId == 0)
            {
                ViewBag.ClassId = new SelectList(_context.Classes.Where(c => c.IsActive), "Id", "Name");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
                ViewBag.SubjectsGroupingId = new SelectList(_context.SubjectsGroupings.Where(sg => sg.IsActive), "Id", "Name");
            }
            else
            {
                ViewBag.ClassId = new SelectList(_context.Classes.Where(c => c.IsActive && c.CampusId == campusId), "Id", "Name");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name");
                ViewBag.SubjectsGroupingId = new SelectList(_context.SubjectsGroupings.Where(sg => sg.IsActive && sg.CampusId == campusId), "Id", "Name");
            }

            return View(student);
        }

        private async Task UpdateSiblingDiscounts(int familyId, decimal newAdmissionDiscount, decimal newTuitionDiscount, int categoryId)
        {
            var siblings = await _context.Students
                .Where(s => s.FamilyId == familyId && !s.HasLeft && s.StudentCategoryId == categoryId)  // ✅ FIX: Only update siblings with same category
                .ToListAsync();

            foreach (var sibling in siblings)
            {
                sibling.AdmissionFeeDiscountAmount = newAdmissionDiscount;
                sibling.TuitionFeeDiscountPercent = newTuitionDiscount;

                var assignment = await _context.StudentCategoryAssignments
                    .FirstOrDefaultAsync(a => a.StudentId == sibling.Id && a.IsActive);

                if (assignment != null)
                {
                    assignment.AppliedAdmissionFeeDiscount = newAdmissionDiscount;
                    assignment.AppliedTuitionFeeDiscount = newTuitionDiscount;
                    assignment.ModifiedDate = DateTime.Now;
                    assignment.ModifiedBy = User.Identity.Name;
                }
            }

            await _context.SaveChangesAsync();
        }

        // API: Check if Father CNIC exists in families
        [HttpGet]
        public async Task<JsonResult> CheckFatherCNIC(string cnic)
        {
            var family = await _context.Families
                .FirstOrDefaultAsync(f => f.FatherCNIC == cnic);

            if (family != null)
            {
                var studentCount = await _context.Students
                    .CountAsync(s => s.FamilyId == family.Id && !s.HasLeft);

                return Json(new
                {
                    exists = true,
                    fatherName = family.FatherName,
                    fatherPhone = family.FatherPhone,
                    motherName = family.MotherName,
                    motherCNIC = family.MotherCNIC,
                    motherPhone = family.MotherPhone,
                    address = family.HomeAddress,
                    fatherSourceOfIncome = family.FatherSourceOfIncome,
                    studentCount = studentCount
                });
            }

            return Json(new { exists = false });
        }

        // API: Check CNIC for category-based discounts
        [HttpGet]
        public async Task<JsonResult> CheckCategoryDiscount(string cnic, int categoryId)
        {
            var category = await _context.StudentCategories
                .Include(c => c.EmployeeCategoryDiscounts)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                return Json(new { success = false, message = "Category not found" });
            }

            var result = new
            {
                success = true,
                categoryType = category.CategoryType,
                categoryName = category.CategoryName,
                data = (object)null
            };

            // Teacher Parent / Employee Parent Logic
            if (category.CategoryType == "EmployeeParent")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.CNIC == cnic && e.IsActive);

                if (employee != null)
                {
                    // Find matching discount for employee role
                    var roleDiscount = category.EmployeeCategoryDiscounts
                        .FirstOrDefault(d => d.EmployeeCategory == employee.Role);

                    if (roleDiscount != null)
                    {
                        return Json(new
                        {
                            success = true,
                            found = true,
                            type = "employee",
                            categoryType = category.CategoryType,
                            employeeId = employee.Id,
                            employeeName = employee.FullName,
                            employeeRole = employee.Role,
                            admissionDiscount = roleDiscount.AdmissionFeeDiscount,
                            tuitionDiscount = roleDiscount.TuitionFeeDiscount,
                            message = $"Employee found: {employee.FullName} ({employee.Role})"
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = true,
                            found = false,
                            type = "employee_no_discount",
                            employeeName = employee.FullName,
                            employeeRole = employee.Role,
                            message = $"Employee found but no discount configured for role: {employee.Role}"
                        });
                    }
                }

                // Not found as employee, prompt for mother/sibling CNIC
                return Json(new
                {
                    success = true,
                    found = false,
                    type = "not_employee",
                    message = "No active employee found with this CNIC. Please enter Mother or Sibling CNIC."
                });
            }

            // Sibling Discount Logic
            // Sibling Discount Logic
            if (category.CategoryType == "Sibling")
            {
                var family = await _context.Families
                    .FirstOrDefaultAsync(f => f.FatherCNIC == cnic);

                if (family != null)
                {
                    var siblings = await _context.Students
                        .Where(s => s.FamilyId == family.Id && !s.HasLeft)
                        .Include(s => s.ClassObj)
                        .ToListAsync();

                    var siblingCount = siblings.Count;

                    // ✅ FIX: Only apply extra discount if siblings meet threshold
                    if (siblingCount >= (category.SiblingCount ?? 1))
                    {
                        var baseAdmissionDiscount = category.DefaultAdmissionFeeDiscount;
                        var baseTuitionDiscount = category.DefaultTuitionFeeDiscount;

                        var perSiblingAdmission = category.PerSiblingAdmissionDiscount ?? 0;
                        var perSiblingTuition = category.PerSiblingTuitionDiscount ?? 0;

                        var totalAdmissionDiscount = baseAdmissionDiscount + (perSiblingAdmission * siblingCount);
                        var totalTuitionDiscount = baseTuitionDiscount + (perSiblingTuition * siblingCount);

                        return Json(new
                        {
                            success = true,
                            found = true,
                            type = "sibling",
                            categoryType = category.CategoryType,
                            siblingCount = siblingCount,
                            siblings = siblings.Select(s => new { s.Id, s.StudentName, Class = s.ClassObj.Name }).ToList(),
                            admissionDiscount = totalAdmissionDiscount,
                            tuitionDiscount = totalTuitionDiscount,
                            message = $"Found {siblingCount} sibling(s) meeting threshold"
                        });
                    }
                }

                // No siblings or below threshold
                return Json(new
                {
                    success = true,
                    found = false,
                    type = "no_sibling",
                    categoryType = category.CategoryType,
                    siblingCount = 0,
                    admissionDiscount = 0,
                    tuitionDiscount = 0,
                    message = "No siblings found or below threshold"
                });
            }

            // Alumni Discount Logic (placeholder - needs Alumni model)
            if (category.CategoryType == "Alumni")
            {
                // Check if Alumni table exists and has matching record
                // For now, return not found
                return Json(new
                {
                    success = true,
                    found = false,
                    type = "alumni",
                    categoryType = category.CategoryType,
                    message = "Alumni validation not yet implemented. Please apply default category discount.",
                    admissionDiscount = category.DefaultAdmissionFeeDiscount,
                    tuitionDiscount = category.DefaultTuitionFeeDiscount
                });
            }

            // Default category - no special validation
            return Json(new
            {
                success = true,
                found = true,
                type = "default",
                categoryType = category.CategoryType,
                admissionDiscount = category.DefaultAdmissionFeeDiscount,
                tuitionDiscount = category.DefaultTuitionFeeDiscount,
                message = "Using default category discount"
            });
        }
        
        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .Include(s => s.SubjectsGrouping)
                .Include(s => s.StudentCategory)
                .Include(s => s.Family)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }
        
        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.Class);
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.Section);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name", student.CampusId);
                ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.SubjectsGroupingId);
            }
            else
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive), "Id", "Name", student.Class);
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive), "Id", "Name", student.Section);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name", student.CampusId);
                ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive), "Id", "Name", student.SubjectsGroupingId);
            }
            
           
            
            // Load student categories for dropdown
            var studentCategories = await _context.StudentCategories
                .Include(c => c.EmployeeCategoryDiscounts)
                .OrderBy(sc => sc.CategoryName)
                .ToListAsync();
            
            ViewData["StudentCategoryId"] = new SelectList(studentCategories, "Id", "CategoryName", student.StudentCategoryId);
            
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student, IFormFile? profilePicture,
            IFormFile? fatherCnicFront, IFormFile? fatherCnicBack, IFormFile? bForm,
            IFormFile? studentCnicFront, IFormFile? studentCnicBack, IFormFile? matricCert1,
            IFormFile? interCert1, IFormFile? matricCert2, IFormFile? interCert2,
            List<int>? OptionalCharges,
            int? SelectedEmployeeId = null,
            int? CalculatedSiblingCount = null,
            string? PaymentMode = null,
            decimal? CustomAdmissionPercent = null,
            decimal? CustomTuitionPercent = null,
            decimal? FatherSalaryPercent = null,
            decimal? MotherSalaryPercent = null,
            int? MotherEmployeeId = null)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            ModelState.Remove(nameof(Student.RegisteredBy));
            ModelState.Remove(nameof(Student.ClassObj));
            ModelState.Remove(nameof(Student.SectionObj));
            ModelState.Remove(nameof(Student.SubjectsGrouping));
            ModelState.Remove(nameof(Student.Campus));
            ModelState.Remove(nameof(Student.Family));
            ModelState.Remove(nameof(Student.StudentCategory));

            // Remove image fields from ModelState validation
            ModelState.Remove("ProfilePicture");
            ModelState.Remove("profilePicture");
            ModelState.Remove("fatherCNICFront");
            ModelState.Remove("fatherCNICBack");
            ModelState.Remove("studentCNICFront");
            ModelState.Remove("studentCNICBack");
            ModelState.Remove("bForm");
            ModelState.Remove("matricCert1");
            ModelState.Remove("matricCert2");
            ModelState.Remove("interCert1");
            ModelState.Remove("interCert2");
            ModelState.Remove("FatherCNIC_Front");
            ModelState.Remove("FatherCNIC_Back");
            ModelState.Remove("StudentCNIC_Front");
            ModelState.Remove("StudentCNIC_Back");
            ModelState.Remove("BForm");
            ModelState.Remove("MatricCertificate_01");
            ModelState.Remove("MatricCertificate_02");
            ModelState.Remove("InterCertificate_01");
            ModelState.Remove("InterCertificate_02");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingStudent = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (existingStudent == null)
                    {
                        return NotFound();
                    }

                    // Handle family update/lookup
                    var family = await _context.Families
                        .FirstOrDefaultAsync(f => f.FatherCNIC == student.FatherCNIC);

                    if (family == null)
                    {
                        family = new Family
                        {
                            FatherName = student.FatherName,
                            FatherCNIC = student.FatherCNIC,
                            FatherPhone = student.FatherPhone,
                            FatherSourceOfIncome = student.FatherSourceOfIncome,
                            MotherName = student.MotherName,
                            MotherCNIC = student.MotherCNIC,
                            MotherPhone = student.MotherPhone,
                            HomeAddress = student.HomeAddress,
                            CampusId = student.CampusId
                        };
                        _context.Families.Add(family);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Update family info
                        family.FatherName = student.FatherName;
                        family.FatherPhone = student.FatherPhone;
                        family.FatherSourceOfIncome = student.FatherSourceOfIncome;
                        family.MotherName = student.MotherName;
                        family.MotherCNIC = student.MotherCNIC;
                        family.MotherPhone = student.MotherPhone;
                        family.HomeAddress = student.HomeAddress;
                        await _context.SaveChangesAsync();
                    }

                    student.FamilyId = family.Id;

                    // Upload new files only if provided, otherwise keep existing
                    if (profilePicture != null)
                        student.ProfilePicture = await student.UploadFile(profilePicture, "profile-pictures", _env);
                    else
                        student.ProfilePicture = existingStudent.ProfilePicture;

                    if (fatherCnicFront != null)
                        student.FatherCNIC_Front = await student.UploadFile(fatherCnicFront, "cnic-documents", _env);
                    else
                        student.FatherCNIC_Front = existingStudent.FatherCNIC_Front;

                    if (fatherCnicBack != null)
                        student.FatherCNIC_Back = await student.UploadFile(fatherCnicBack, "cnic-documents", _env);
                    else
                        student.FatherCNIC_Back = existingStudent.FatherCNIC_Back;

                    if (studentCnicFront != null)
                        student.StudentCNIC_Front = await student.UploadFile(studentCnicFront, "cnic-documents", _env);
                    else
                        student.StudentCNIC_Front = existingStudent.StudentCNIC_Front;

                    if (studentCnicBack != null)
                        student.StudentCNIC_Back = await student.UploadFile(studentCnicBack, "cnic-documents", _env);
                    else
                        student.StudentCNIC_Back = existingStudent.StudentCNIC_Back;

                    if (bForm != null)
                        student.BForm = await student.UploadFile(bForm, "bform-documents", _env);
                    else
                        student.BForm = existingStudent.BForm;

                    if (matricCert1 != null)
                        student.MatricCertificate_01 = await student.UploadFile(matricCert1, "certificates", _env);
                    else
                        student.MatricCertificate_01 = existingStudent.MatricCertificate_01;

                    if (matricCert2 != null)
                        student.MatricCertificate_02 = await student.UploadFile(matricCert2, "certificates", _env);
                    else
                        student.MatricCertificate_02 = existingStudent.MatricCertificate_02;

                    if (interCert1 != null)
                        student.InterCertificate_01 = await student.UploadFile(interCert1, "certificates", _env);
                    else
                        student.InterCertificate_01 = existingStudent.InterCertificate_01;

                    if (interCert2 != null)
                        student.InterCertificate_02 = await student.UploadFile(interCert2, "certificates", _env);
                    else
                        student.InterCertificate_02 = existingStudent.InterCertificate_02;

                    // Preserve original registration data
                    student.RegisteredBy = existingStudent.RegisteredBy;
                    student.RegistrationDate = existingStudent.RegistrationDate;
                    student.AdmissionFeePaid = existingStudent.AdmissionFeePaid;
                    student.HasLeft = existingStudent.HasLeft;

                    // Check if class or section changed - regenerate roll number if needed
                    bool classOrSectionChanged = existingStudent.Class != student.Class || existingStudent.Section != student.Section;
                    
                    _context.Update(student);
                    await _context.SaveChangesAsync();

                    // Regenerate roll number if class or section changed
                    if (classOrSectionChanged)
                    {
                        student.RollNumber = await GenerateRollNumber(student);
                        _context.Update(student);
                        await _context.SaveChangesAsync();
                    }

                    // Update username if CNIC changed
                    bool cnicChanged = existingStudent.StudentCNIC != student.StudentCNIC;
                    if (cnicChanged && !string.IsNullOrWhiteSpace(student.StudentCNIC))
                    {
                        var applicationUser = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.StudentId == student.Id);
                        
                        if (applicationUser != null)
                        {
                            var newUsername = student.StudentCNIC.Replace("-", "");
                            
                            // Check if new username is already taken
                            var existingUser = await _userManager.FindByNameAsync(newUsername);
                            if (existingUser == null || existingUser.Id == applicationUser.Id)
                            {
                                applicationUser.UserName = newUsername;
                                applicationUser.NormalizedUserName = newUsername.ToUpper();
                                var updateResult = await _userManager.UpdateAsync(applicationUser);
                                
                                if (!updateResult.Succeeded)
                                {
                                    TempData["Warning"] = "Student updated but username update failed. CNIC changed.";
                                }
                            }
                            else
                            {
                                TempData["Warning"] = "Student updated but username could not be changed as the new CNIC is already in use by another user.";
                            }
                        }
                    }

                    // Log admin notification for student edit
                    await LogAdminNotification(
                        action: "Student Edited",
                        description: $"Student '{student.StudentName}' (Roll No: {student.RollNumber}) was updated by {User.Identity.Name}" + 
                                     (classOrSectionChanged ? " - Class/Section changed, roll number regenerated" : ""),
                        studentId: student.Id,
                        campusId: student.CampusId,
                        entityType: "Student",
                        entityId: student.Id
                    );

                    // Update optional charges
                    if (OptionalCharges != null)
                    {
                        var existingAssignments = await _context.StudentChargeAssignments
                            .Where(sca => sca.StudentId == student.Id)
                            .ToListAsync();

                        // Get all optional charges for the class
                        var allClassCharges = await _context.ClassFeeExtraCharges
                            .Where(c => c.ClassId == student.Class && c.Category == "Optional")
                            .Select(c => c.Id)
                            .ToListAsync();

                        foreach (var chargeId in allClassCharges)
                        {
                            var existingAssignment = existingAssignments.FirstOrDefault(a => a.ClassFeeExtraChargeId == chargeId);
                            var isAssigned = OptionalCharges.Contains(chargeId);

                            if (existingAssignment != null)
                            {
                                existingAssignment.IsAssigned = isAssigned;
                            }
                            else
                            {
                                _context.StudentChargeAssignments.Add(new StudentChargeAssignment
                                {
                                    StudentId = student.Id,
                                    ClassFeeExtraChargeId = chargeId,
                                    IsAssigned = isAssigned
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    // Handle StudentCategory assignment update
                    if (student.StudentCategoryId.HasValue)
                    {
                        var category = await _context.StudentCategories
                            .Include(c => c.EmployeeCategoryDiscounts)
                            .FirstOrDefaultAsync(c => c.Id == student.StudentCategoryId.Value);

                        if (category != null)
                        {
                            // Validation for Employee Parent category with salary deduction
                            if (category.CategoryType == "EmployeeParent" && PaymentMode == "DeductFromSalary")
                            {
                                if (!SelectedEmployeeId.HasValue)
                                {
                                    ModelState.AddModelError("", "Employee must be selected for Employee Parent category with salary deduction.");
                                }
                                else
                                {
                                    // Check if employee has a salary defined
                                    var employeeSalary = await _context.SalaryDefinitions
                                        .Where(sd => sd.EmployeeId == SelectedEmployeeId.Value && sd.IsActive)
                                        .FirstOrDefaultAsync();
                                    
                                    if (employeeSalary == null)
                                    {
                                        ModelState.AddModelError("", "Selected employee does not have a salary defined. Please define salary first.");
                                    }
                                    else
                                    {
                                        // Get class fee to check if salary is sufficient
                                        var classFee = await _context.ClassFees
                                            .FirstOrDefaultAsync(cf => cf.ClassId == student.Class);
                                        
                                        if (classFee != null)
                                        {
                                            var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                                            var deductionPercent = CustomTuitionPercent ?? 40m; // Default 40% if not specified
                                            var deductionAmount = (tuitionFee * deductionPercent) / 100m;
                                            
                                            // Check if net salary is sufficient for deduction
                                            if (employeeSalary.NetSalary < deductionAmount)
                                            {
                                                ModelState.AddModelError("", $"Employee's net salary ({employeeSalary.NetSalary:N2}) is insufficient to cover the fee deduction amount ({deductionAmount:N2}).");
                                            }
                                        }
                                    }
                                }
                            }
                            
                            // Only proceed if validation passed
                            if (!ModelState.IsValid)
                            {
                                // Repopulate dropdowns for the view
                                if (campusId.HasValue && campusId.Value > 0)
                                {
                                    ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.Class);
                                    ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.Section);
                                    ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId), "Id", "Name", student.CampusId);
                                    ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive && cs.CampusId == campusId), "Id", "Name", student.SubjectsGroupingId);
                                }
                                else
                                {
                                    ViewData["ClassId"] = new SelectList(_context.Classes.Where(cs => cs.IsActive), "Id", "Name", student.Class);
                                    ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive), "Id", "Name", student.Section);
                                    ViewData["CampusId"] = new SelectList(_context.Campuses.Where(cs => cs.IsActive), "Id", "Name", student.CampusId);
                                    ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(cs => cs.IsActive), "Id", "Name", student.SubjectsGroupingId);
                                }
                                return View(student);
                            }
                            
                            // Check if there's an existing active assignment
                            var existingAssignment = await _context.StudentCategoryAssignments
                                .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.IsActive);

                            if (existingAssignment != null)
                            {
                                // Scenario 1: Category changed - Update everything
                                if (existingAssignment.StudentCategoryId != category.Id)
                                {
                                    existingAssignment.StudentCategoryId = category.Id;
                                    existingAssignment.EmployeeId = SelectedEmployeeId;
                                    existingAssignment.PaymentMode =  PaymentMode ?? "0";
                                    existingAssignment.CustomAdmissionPercent = CustomAdmissionPercent;
                                    existingAssignment.CustomTuitionPercent = CustomTuitionPercent;
                                }
                                // Scenario 2: Category is the same - Conditional update
                                else
                                {
                                    if (SelectedEmployeeId != null)
                                        existingAssignment.EmployeeId = SelectedEmployeeId;

                                    if (PaymentMode != null)
                                        existingAssignment.PaymentMode = PaymentMode;

                                    if (CustomAdmissionPercent != null)
                                    {
                                        existingAssignment.CustomAdmissionPercent = CustomAdmissionPercent;
                                        // Automatically update Tuition Percent based on your formula
                                        existingAssignment.CustomTuitionPercent = 1 - CustomAdmissionPercent;
                                    }
                                }

                                // Always update these common fields
                                existingAssignment.AppliedAdmissionFeeDiscount = student.AdmissionFeeDiscountAmount ?? 0;
                                existingAssignment.AppliedTuitionFeeDiscount = student.TuitionFeeDiscountPercent ?? 0;
                                existingAssignment.ModifiedDate = DateTime.Now;
                                existingAssignment.ModifiedBy = User.Identity.Name;
                            }
                            else
                            {
                                // Create new assignment
                                var assignment = new StudentCategoryAssignment
                                {
                                    StudentId = student.Id,
                                    StudentCategoryId = category.Id,
                                    EmployeeId = SelectedEmployeeId,
                                    PaymentMode = PaymentMode ?? "0",
                                    CustomAdmissionPercent = CustomAdmissionPercent,
                                    CustomTuitionPercent = CustomTuitionPercent,
                                    AppliedAdmissionFeeDiscount = student.AdmissionFeeDiscountAmount ?? 0,
                                    AppliedTuitionFeeDiscount = student.TuitionFeeDiscountPercent ?? 0,
                                    AssignedDate = DateTime.Now,
                                    AssignedBy = User.Identity.Name,
                                    IsActive = true,
                                    ModifiedBy = "",

                                };
                                _context.StudentCategoryAssignments.Add(assignment);
                            }

                            await _context.SaveChangesAsync();

                            // Update sibling discounts if Sibling category
                            if (category.CategoryType == "Sibling" && CalculatedSiblingCount.HasValue && CalculatedSiblingCount.Value > 0)
                            {
                                await UpdateSiblingDiscounts(student.FamilyId.Value,
                                    student.AdmissionFeeDiscountAmount ?? 0,
                                    student.TuitionFeeDiscountPercent ?? 0,
                                    category.Id);
                            }
                        }
                    }

                    TempData["SuccessMessage"] = "Student updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            // Repopulate ViewBag on error
            if (campusId == null || campusId == 0)
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive), "Id", "Name", student.Class);
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(c => c.IsActive), "Id", "Name", student.Section);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", student.CampusId);
                ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(sg => sg.IsActive), "Id", "Name", student.SubjectsGroupingId);
            }
            else
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive && c.CampusId == campusId), "Id", "Name", student.Class);
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(c => c.IsActive && c.CampusId == campusId), "Id", "Name", student.Section);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", student.CampusId);
                ViewData["SubjectsGroupingId"] = new SelectList(_context.SubjectsGroupings.Where(sg => sg.IsActive && sg.CampusId == campusId), "Id", "Name", student.SubjectsGroupingId);
            }

            return View(student);
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
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

        // Helper method to log admin notifications
        private async Task LogAdminNotification(string action, string description, int? studentId = null, int? pickupCardId = null, int? campusId = null, string entityType = null, int? entityId = null)
        {
            try
            {
                var notification = new AdminNotification
                {
                    Action = action,
                    Description = description,
                    ActionDate = DateTime.Now,
                    PerformedBy = User.Identity.Name ?? "System", // Fallback if user is not logged in
                    StudentId = studentId,
                    PickupCardId = pickupCardId,
                    CampusId = campusId,
                    EntityType = entityType,
                    EntityId = entityId,
                    IsRead = false
                };

                _context.AdminNotifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error to your console or debug window
                Console.WriteLine($"Error logging admin notification: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                // Optional: Re-throw if you want the calling method to know it failed
                // throw; 
            }
        }

        // GET: Students
        public async Task<IActionResult> IDCard(int id)
        {
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            // Get current academic year from database (or use current year as fallback)
            var currentAcademicYear = await _context.AcademicYear
                .OrderByDescending(ay => ay.Year)
                .FirstOrDefaultAsync();
            
            ViewBag.AcademicYear = currentAcademicYear != null 
                ? $"{currentAcademicYear.Year}-{currentAcademicYear.Year + 1}"
                : $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";

            return View(student);
        }

        // API: Get pickup cards for student
        [HttpGet]
        public async Task<JsonResult> GetPickupCards(int studentId)
        {
            var cards = await _context.PickupCards
                .Where(pc => pc.StudentId == studentId)
                .OrderByDescending(pc => pc.CreatedDate)
                .Select(pc => new
                {
                    id = pc.Id,
                    personName = pc.PersonName,
                    cnic = pc.CNIC,
                    relation = pc.Relation,
                    isActive = pc.IsActive,
                    createdDate = pc.CreatedDate
                })
                .ToListAsync();

            return Json(cards);
        }

        // POST: Students/AddPickupCard
        [HttpPost]
        public async Task<JsonResult> AddPickupCard(IFormFile? personPicture, IFormFile? cnicPicture)
        {
            try
            {
                var studentId = int.Parse(Request.Form["studentId"]);
                var personName = Request.Form["personName"].ToString();
                var cnic = Request.Form["cnic"].ToString();
                var relation = Request.Form["relation"].ToString();

                var pickupCard = new PickupCard
                {
                    StudentId = studentId,
                    PersonName = personName,
                    CNIC = cnic,
                    Relation = relation,
                    CreatedBy = User.Identity.Name,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                // Upload files
                if (personPicture != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pickup-cards");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(personPicture.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await personPicture.CopyToAsync(fileStream);
                    }

                    pickupCard.PersonPicture = "uploads/pickup-cards/" + uniqueFileName;
                }

                if (cnicPicture != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pickup-cards");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(cnicPicture.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await cnicPicture.CopyToAsync(fileStream);
                    }

                    pickupCard.CNICPicture = "uploads/pickup-cards/" + uniqueFileName;
                }

                _context.PickupCards.Add(pickupCard);
                await _context.SaveChangesAsync();

                // Log admin notification for pickup card addition
                var student = await _context.Students.FindAsync(studentId);
                await LogAdminNotification(
                    action: "Pickup Card Added",
                    description: $"New pickup card for '{pickupCard.PersonName}' was added for student '{student?.StudentName}' by {User.Identity.Name}",
                    studentId: studentId,
                    pickupCardId: pickupCard.Id,
                    campusId: student?.CampusId,
                    entityType: "PickupCard",
                    entityId: pickupCard.Id
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Students/EditPickupCard
        [HttpPost]
        public async Task<JsonResult> EditPickupCard(IFormFile? personPicture, IFormFile? cnicPicture)
        {
            try
            {
                var cardId = int.Parse(Request.Form["cardId"]);
                var personName = Request.Form["personName"].ToString();
                var cnic = Request.Form["cnic"].ToString();
                var relation = Request.Form["relation"].ToString();

                var pickupCard = await _context.PickupCards.FindAsync(cardId);
                if (pickupCard == null)
                {
                    return Json(new { success = false, message = "Card not found" });
                }

                pickupCard.PersonName = personName;
                pickupCard.CNIC = cnic;
                pickupCard.Relation = relation;
                pickupCard.ModifiedBy = User.Identity.Name;
                pickupCard.ModifiedDate = DateTime.Now;

                // Upload new person picture if provided
                if (personPicture != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pickup-cards");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(personPicture.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await personPicture.CopyToAsync(fileStream);
                    }

                    pickupCard.PersonPicture = "uploads/pickup-cards/" + uniqueFileName;
                }

                // Upload new CNIC picture if provided
                if (cnicPicture != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pickup-cards");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(cnicPicture.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await cnicPicture.CopyToAsync(fileStream);
                    }

                    pickupCard.CNICPicture = "uploads/pickup-cards/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();

                // Log admin notification for pickup card edit
                var student = await _context.Students.FindAsync(pickupCard.StudentId);
                await LogAdminNotification(
                    action: "Pickup Card Edited",
                    description: $"Pickup card for '{pickupCard.PersonName}' was updated for student '{student?.StudentName}' by {User.Identity.Name}",
                    studentId: pickupCard.StudentId,
                    pickupCardId: pickupCard.Id,
                    campusId: student?.CampusId,
                    entityType: "PickupCard",
                    entityId: pickupCard.Id
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Students/DeactivatePickupCard
        [HttpPost]
        public async Task<JsonResult> DeactivatePickupCard(int id)
        {
            try
            {
                var card = await _context.PickupCards.FindAsync(id);
                if (card == null)
                {
                    return Json(new { success = false, message = "Card not found" });
                }

                card.IsActive = false;
                card.ModifiedBy = User.Identity.Name;
                card.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Students/ActivatePickupCard
        [HttpPost]
        public async Task<JsonResult> ActivatePickupCard(int id)
        {
            try
            {
                var card = await _context.PickupCards.FindAsync(id);
                if (card == null)
                {
                    return Json(new { success = false, message = "Card not found" });
                }

                card.IsActive = true;
                card.ModifiedBy = User.Identity.Name;
                card.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Students/PrintPickupCard/5
        public async Task<IActionResult> PrintPickupCard(int id)
        {
            var card = await _context.PickupCards
                .Include(pc => pc.Student)
                .ThenInclude(s => s.ClassObj)
                .Include(pc => pc.Student.SectionObj)
                .Include(pc => pc.Student.Campus)
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            return View(card);
        }
        
        // API: Get employees by campus
        [HttpGet]
        public async Task<JsonResult> GetEmployeesByCampus(int? campusId)
        {
            var query = _context.Employees.Where(e => e.IsActive);
            
            if (campusId.HasValue && campusId.Value > 0)
            {
                query = query.Where(e => e.CampusId == campusId);
            }
            
            var employees = await query
                .Select(e => new
                {
                    id = e.Id,
                    name = e.FullName,
                    role = e.Role,
                    cnic = e.CNIC
                })
                .OrderBy(e => e.name)
                .ToListAsync();
                
            return Json(employees);
        }
        
        // API: Check sibling count for family
        [HttpGet]
        public async Task<JsonResult> CheckSiblingCount(string fatherCNIC)
        {
            if (string.IsNullOrWhiteSpace(fatherCNIC))
            {
                return Json(new { count = 0 });
            }
            
            var family = await _context.Families
                .Include(f => f.Students.Where(s => !s.HasLeft))
                .ThenInclude(s => s.ClassObj)
                .Include(f => f.Students)
                .ThenInclude(s => s.Campus)
                .FirstOrDefaultAsync(f => f.FatherCNIC == fatherCNIC && f.IsActive);
                
            if (family != null)
            {
                return Json(new
                {
                    count = family.Students.Count,
                    students = family.Students.Select(s => new
                    {
                        id = s.Id,
                        name = s.StudentName,
                        className = s.ClassObj != null ? s.ClassObj.Name : "",
                        campusName = s.Campus != null ? s.Campus.Name : "",
                        admissionDiscount = s.AdmissionFeeDiscountAmount,
                        tuitionDiscount = s.TuitionFeeDiscountPercent
                    }).ToList()
                });
            }
            
            return Json(new { count = 0, students = new List<object>() });
        }
        
        // GET: Check student outstanding dues
        [HttpGet]
        public async Task<JsonResult> CheckStudentDues(int studentId)
        {
            var latestBilling = await _context.BillingMaster
                .Where(b => b.StudentId == studentId)
                .OrderByDescending(b => b.CreatedDate)
                .FirstOrDefaultAsync();

            decimal outstandingDues = latestBilling?.Dues ?? 0;

            return Json(new
            {
                hasDues = outstandingDues > 0,
                duesAmount = outstandingDues
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetStudentAssignedCharges(int studentId)
        {
            var assignedCharges = await _context.StudentChargeAssignments
                .Where(sca => sca.StudentId == studentId)
                .Select(sca => new
                {
                    chargeId = sca.ClassFeeExtraChargeId,
                    isAssigned = sca.IsAssigned
                })
                .ToListAsync();

            return Json(assignedCharges);
        }

        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(s => s.ClassId == classId && s.IsActive)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name
                })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(sections);
        }

        [HttpGet]
        public async Task<JsonResult> GetSectionsByClassName(string className)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            var query = _context.ClassSections
                .Include(s => s.Class)
                .Where(s => s.Class.Name == className && s.IsActive);

            // Filter by campus if user is not owner
            if (userCampusId.HasValue && userCampusId.Value != 0)
            {
                query = query.Where(s => s.CampusId == userCampusId.Value);
            }

            var sections = await query
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name
                })
                .Distinct()
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(sections);
        }
    }
}