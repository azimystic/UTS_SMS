using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;
using System.Text.Json;

namespace UTS_SMS.Controllers
{
    public class ExamMarksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ExamMarksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // Helper method to parse academic year from string (e.g., "2025-2026" -> 2025)
        private int ParseAcademicYear(string academicYearString)
        {
            if (string.IsNullOrEmpty(academicYearString))
            {
                return DateTime.Now.Year;
            }

            var yearParts = academicYearString.Split('-');
            if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
            {
                return parsedYear;
            }

            return DateTime.Now.Year;
        }

        // GET: ExamMarks
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            var viewModel = new ExamMarksAnalysisViewModel
            {
                ExamCategories = await _context.ExamCategories.Where(x => x.IsActive && x.CampusId == campusId).ToListAsync(),
                Classes = await _context.Classes.Where(x => x.IsActive && x.CampusId == campusId).ToListAsync(),
                Subjects = await _context.Subjects.Where(x => x.IsActive && x.CampusId == campusId).ToListAsync()
            };

            return View(viewModel);
        }

        // GET: ExamMarks/Entry
        public async Task<IActionResult> Entry()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var campusId = 0;
            if (currentUser.CampusId != null) {
                campusId = (int)currentUser.CampusId;
            }
            
            var allCampus = await _context.Campuses.Where(x => x.IsActive).ToListAsync();
            if (campusId != 0)
            {
                allCampus = allCampus.Where(x => x.Id == campusId).ToList();
            }

            var isTeacher = User.IsInRole("Teacher");
            List<Class> allClasses;
            List<Subject> allSubjects;

            // Filter classes and subjects for teachers
            if (isTeacher && currentUser.EmployeeId.HasValue)
            {
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == currentUser.EmployeeId.Value && ta.IsActive)
                    .Include(ta => ta.Class)
                    .Include(ta => ta.Subject)
                    .ToListAsync();

                var teacherClassIds = teacherAssignments.Select(ta => ta.ClassId).Distinct().ToList();
                var teacherSubjectIds = teacherAssignments.Select(ta => ta.SubjectId).Distinct().ToList();

                allClasses = await _context.Classes
                    .Where(x => x.IsActive && teacherClassIds.Contains(x.Id))
                    .ToListAsync();

                allSubjects = await _context.Subjects
                    .Where(x => x.IsActive && teacherSubjectIds.Contains(x.Id))
                    .ToListAsync();
            }
            else
            {
                // Admin sees all classes and subjects
                allClasses = await _context.Classes.Where(x => x.IsActive).ToListAsync();
                allSubjects = await _context.Subjects.Where(x => x.IsActive).ToListAsync();
            }
            
            var viewModel = new ExamMarksEntryViewModel
            {
                ExamCategories = await _context.ExamCategories.Where(x => x.IsActive ).ToListAsync(),
                Campuses = allCampus,
                Classes = allClasses,
                Subjects = allSubjects,
                AcademicYears = await _context.AcademicYear.OrderByDescending(ay => ay.Year).ToListAsync()
            };

            return View(viewModel);
        }

        // GET: Get Exams by Category
        [HttpGet]
        public async Task<JsonResult> GetExamsByCategory(int categoryId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            var exams = await _context.Exams
                .Where(x => x.ExamCategoryId == categoryId && x.IsActive)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            return Json(exams);
        }
        [HttpGet]
        public async Task<JsonResult> GetClassesByCategory(int campusId)
        {
            var classes = await _context.Classes
                .Where(x => x.CampusId == campusId && x.IsActive)
                .Select(x => new { id = x.Id, name = x.Name })
                .ToListAsync();

            return Json(classes);
        }


        // GET: Get Sections by Class
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            var isTeacher = User.IsInRole("Teacher");

            IQueryable<ClassSection> sectionsQuery = _context.ClassSections
                .Where(x => x.ClassId == classId && x.IsActive);

            // Filter sections for teachers based on their assignments
            if (isTeacher && currentUser.EmployeeId.HasValue)
            {
                var teacherSectionIds = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == currentUser.EmployeeId.Value && 
                                ta.ClassId == classId && 
                                ta.IsActive)
                    .Select(ta => ta.SectionId)
                    .Distinct()
                    .ToListAsync();

                sectionsQuery = sectionsQuery.Where(s => teacherSectionIds.Contains(s.Id));
            }

            var sections = await sectionsQuery
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            return Json(sections);
        }

        // GET: Get Academic Year by Class
        [HttpGet]
        public async Task<JsonResult> GetAcademicYearByClass(int classId)
        {
            // Validate parameter
            if (classId <= 0)
            {
                return Json(new { success = false, message = "Invalid class ID" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            
            var classEntity = await _context.Classes.FindAsync(classId);
            if (classEntity == null)
            {
                return Json(new { success = false, message = "Class not found" });
            }
            
            // Authorization: Check if user has access to this class's campus
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isOwner = userRoles.Contains("Owner");
            
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                if (classEntity.CampusId != userCampusId.Value)
                {
                    return Json(new { success = false, message = "Access denied" });
                }
            }
            
            if (!string.IsNullOrEmpty(classEntity.CurrentAcademicYear))
            {
                int parsedYear = ParseAcademicYear(classEntity.CurrentAcademicYear);
                return Json(new { success = true, academicYear = parsedYear });
            }
            
            return Json(new { success = false });
        }

        // GET: Get Students for Marks Entry
        [HttpGet]
        public async Task<JsonResult> GetStudentsForMarksEntry(int campusId, int examId, int classId, int sectionId, int subjectId, int? academicYear)
        { 
            try
            {
                // Get the class to check its current academic year
                var classEntity = await _context.Classes.FindAsync(classId);
                if (classEntity == null)
                {
                    return Json(new { success = false, message = "Class not found." });
                }

                // Determine the academic year to use
                int targetAcademicYear = 0;
                if (academicYear.HasValue && academicYear.Value > 0)
                {
                    // User selected a specific year
                    targetAcademicYear = academicYear.Value;
                }
                else if (!string.IsNullOrEmpty(classEntity.CurrentAcademicYear))
                {
                    // Default to class's current academic year
                    var yearParts = classEntity.CurrentAcademicYear.Split('-');
                    if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                    {
                        targetAcademicYear = parsedYear;
                    }
                }

                // ✅ CHECK IF EXAM IS DEFINED IN DATE SHEET FIRST
                var examDateSheet = await _context.ExamDateSheets
                    .Where(eds => eds.ExamId == examId && eds.SubjectId == subjectId && 
                                  eds.CampusId == campusId && eds.IsActive && 
                                  eds.AcademicYear == targetAcademicYear)
                    .Include(eds => eds.ClassSections)
                    .FirstOrDefaultAsync(eds => eds.ClassSections.Any(cs => cs.ClassId == classId && cs.SectionId == sectionId));

                if (examDateSheet == null)
                {
                    return Json(new { 
                        success = false, 
                        requiresDateSheet = true,
                        message = $"⚠️ This exam is not defined in the Date Sheet for the selected configuration and academic year {targetAcademicYear}. Please add it to the Exam Date Sheet first before entering marks."
                    });
                }

                // Get all students in the selected class and section
                var students = await _context.Students
                    .Where(s => s.Class == classId && s.Section == sectionId && !s.HasLeft && s.CampusId == campusId)
                    .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                    .ThenInclude(sgd => sgd.Subject)
                    .ToListAsync();

                // VERSIONING LOGIC: Get existing exam marks for the selected academic year only
                var existingMarks = await _context.ExamMarks
                    .Where(em => em.ExamId == examId && em.SubjectId == subjectId &&
                                em.ClassId == classId && em.SectionId == sectionId && 
                                em.CampusId == campusId && em.AcademicYear == targetAcademicYear)
                    .ToListAsync();
                
                // Use values from exam date sheet
                var totalMarks = examDateSheet.TotalMarks;
                var passingMarks = examDateSheet.PassingMarks;
                var examDate = examDateSheet.ExamDate;
                
                var studentMarks = students.Select(student =>
                {
                    var existingMark = existingMarks.FirstOrDefault(em => em.StudentId == student.Id && em.CampusId == campusId);
                    var isEnrolledInSubject = student.SubjectsGrouping?.SubjectsGroupingDetails?
                        .Any(sgd => sgd.SubjectId == subjectId && sgd.IsActive ) ?? false;

                    return new StudentMarksEntry
                    {
                        StudentId = student.Id,
                        StudentName = student.StudentName,
                        FatherName = student.FatherName,
                        IsEnrolledInSubject = isEnrolledInSubject,
                        ObtainedMarks = existingMark?.ObtainedMarks ?? 0,
                        Status = existingMark?.Status ?? "",
                        Grade = existingMark?.Grade ?? "",
                        Percentage = existingMark?.Percentage ?? 0,
                        HasExistingMarks = existingMark != null,
                        ExamMarksId = existingMark?.Id
                    };
                }).ToList();

                var response = new { 
                    success = true, 
                    students = studentMarks,
                    totalMarks = totalMarks,
                    passingMarks = passingMarks,
                    examDate = examDate,
                    academicYear = targetAcademicYear,
                    hasExamDateSheet = true
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Save Exam Marks
        [HttpPost]
        public async Task<JsonResult> SaveExamMarks([FromBody] SaveExamMarksRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var examMarksToSave = new List<ExamMarks>();
                var examMarksToUpdate = new List<ExamMarks>();

                // Fetch the current academic year from the ClassSection
                var classSection = await _context.ClassSections
                    .Where(cs => cs.Id == request.SectionId && cs.ClassId == request.ClassId)
                    .FirstOrDefaultAsync();

                if (classSection == null)
                {
                    return Json(new { success = false, message = "Class section not found." });
                }

                // Use the ClassSection's current academic year, or fall back to current year as integer
                int academicYear;
                if (!string.IsNullOrEmpty(classSection.CurrentAcademicYear))
                {
                    academicYear = ParseAcademicYear(classSection.CurrentAcademicYear);
                }
                else
                {
                    academicYear = DateTime.Now.Year; // Fallback if not set
                }

                foreach (var studentMark in request.StudentMarks.Where(sm => sm.IsEnrolledInSubject))
                {
                    ExamMarks examMarks;

                    if (studentMark.HasExistingMarks && studentMark.ExamMarksId.HasValue)
                    {
                        examMarks = await _context.ExamMarks.FindAsync(studentMark.ExamMarksId.Value);
                        if (examMarks == null) continue;
                        examMarks.CampusId = request.CampusId;
                        examMarks.ObtainedMarks = studentMark.ObtainedMarks;
                        examMarks.AcademicYear = academicYear; // Use the class section's academic year
                        examMarks.ModifiedDate = DateTime.Now;
                        examMarks.ModifiedBy = User.Identity?.Name ?? "System";
                    }
                    else
                    {
                        examMarks = new ExamMarks
                        {
                            StudentId = studentMark.StudentId,
                            ExamId = request.ExamId,
                            SubjectId = request.SubjectId,
                            CampusId = request.CampusId,
                            ClassId = request.ClassId,
                            SectionId = request.SectionId,
                            TotalMarks = request.TotalMarks,
                            PassingMarks = request.PassingMarks,
                            ObtainedMarks = studentMark.ObtainedMarks,
                            ExamDate = request.ExamDate,
                            AcademicYear = academicYear, // Use the class section's academic year
                            CreatedBy = User.Identity?.Name ?? "System"
                        };
                    }

                    // Calculate status and grade
                    examMarks.TotalMarks = request.TotalMarks;
                    examMarks.PassingMarks = request.PassingMarks;
                    examMarks.CalculateStatusAndGrade();

                    if (studentMark.HasExistingMarks)
                    {
                        examMarksToUpdate.Add(examMarks);
                    }
                    else
                    {
                        examMarksToSave.Add(examMarks);
                    }
                }

                if (examMarksToSave.Any())
                {
                    await _context.ExamMarks.AddRangeAsync(examMarksToSave);
                }

                if (examMarksToUpdate.Any())
                {
                    _context.ExamMarks.UpdateRange(examMarksToUpdate);
                }

                await _context.SaveChangesAsync();

                // Create notifications for students when marks are entered
                foreach (var examMark in examMarksToSave.Concat(examMarksToUpdate))
                {
                    await _notificationService.CreateMarksEntryNotification(
                        examMark.StudentId,
                        examMark.ExamId,
                        examMark.SubjectId,
                        examMark.ObtainedMarks,
                        examMark.TotalMarks,
                        examMark.CampusId,
                        User.Identity?.Name ?? "System"
                    );
                }

                return Json(new { success = true, message = "Exam marks saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: View Exam Marks Analysis
        public async Task<IActionResult> Analysis(int? examCategoryId, int? examId, int? campusId, int? classId, int? sectionId, int? subjectId, int? studentId, int? academicYear, string filterMode)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check if user is owner
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isOwner = userRoles.Contains("Owner");
            
            // Default to classwise if not specified
            if (string.IsNullOrEmpty(filterMode))
            {
                filterMode = "classwise";
            }
            
            var usercampusId = campusId;
            if (campusId == null)
            {
                if(currentUser.CampusId != null)
                {
                    usercampusId = (int)currentUser.CampusId;
                }
            }
            
            var allCampuses = await _context.Campuses.Where(x => x.IsActive).ToListAsync();
            
            // If not owner, restrict to user's campus
            if(!isOwner && currentUser.CampusId != null)
            {
                allCampuses = allCampuses.Where(x => x.Id == currentUser.CampusId).ToList();
            }
            
            var viewModel = new ExamMarksAnalysisViewModel
            {
                ExamCategoryId = examCategoryId,
                ExamId = examId,
                ClassId = classId,
                CampusId = campusId,
                SectionId = sectionId,
                SubjectId = subjectId,
                StudentId = studentId,
                AcademicYear = academicYear,
                FilterMode = filterMode,
                IsOwner = isOwner,
                ExamCategories = await _context.ExamCategories.Where(x => x.IsActive).ToListAsync(),
                Classes = await _context.Classes.Where(x => x.IsActive).ToListAsync(),
                Campuses = allCampuses,
                Subjects = await _context.Subjects.Where(x => x.IsActive).ToListAsync(),
                AcademicYears = await _context.AcademicYear.OrderByDescending(ay => ay.Year).ToListAsync()
            };
            
            // Auto-select academic year based on filter mode
            if (!academicYear.HasValue)
            {
                if (filterMode == "classwise" && classId.HasValue)
                {
                    // Class-Wise: Default to the class's current academic year
                    var classEntity = await _context.Classes.FindAsync(classId.Value);
                    if (classEntity != null && !string.IsNullOrEmpty(classEntity.CurrentAcademicYear))
                    {
                        int parsedYear = ParseAcademicYear(classEntity.CurrentAcademicYear);
                        viewModel.AcademicYear = parsedYear;
                        academicYear = parsedYear;
                    }
                }
                else if (filterMode == "testwise")
                {
                    // Test-Wise: Default to the latest academic year in the system
                    var latestYear = await _context.AcademicYear
                        .OrderByDescending(ay => ay.Year)
                        .FirstOrDefaultAsync();
                    
                    if (latestYear != null)
                    {
                        viewModel.AcademicYear = latestYear.Year;
                        academicYear = latestYear.Year;
                    }
                }
            }
            
            // Load student name and academic year if studentId is provided
            if (studentId.HasValue)
            {
                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .FirstOrDefaultAsync(s => s.Id == studentId.Value);
                    
                if (student != null)
                {
                    viewModel.StudentName = student.StudentName;
                    
                    // Set academic year for studentwise mode if not already set
                    if (filterMode == "studentwise" && !academicYear.HasValue)
                    {
                        if (student.ClassObj != null && !string.IsNullOrEmpty(student.ClassObj.CurrentAcademicYear))
                        {
                            int parsedYear = ParseAcademicYear(student.ClassObj.CurrentAcademicYear);
                            viewModel.AcademicYear = parsedYear;
                            academicYear = parsedYear;
                        }
                    }
                }
            }

            // Load exams based on category
            if (examCategoryId.HasValue)
            {
                viewModel.Exams = await _context.Exams
                    .Where(x => x.ExamCategoryId == examCategoryId.Value && x.IsActive)
                    .ToListAsync();
            }
            
            // Load classes based on campus
            if (usercampusId.HasValue)
            {
                viewModel.Classes = await _context.Classes
                    .Where(x => x.CampusId == usercampusId && x.IsActive)
                    .ToListAsync();
            }
            
            // Load sections based on class
            if (classId.HasValue)
            {
                viewModel.Sections = await _context.ClassSections
                    .Where(x => x.ClassId == classId.Value && x.IsActive && x.CampusId == usercampusId)
                    .ToListAsync();
            }

            // Load exam marks based on filters and mode
            bool shouldLoadData = false;
            
            // Validation based on filter mode
            if (filterMode == "classwise")
            {
                // Class-wise requires: Exam Category (mandatory), Class (mandatory)
                shouldLoadData = examCategoryId.HasValue && classId.HasValue;
            }
            else if (filterMode == "testwise")
            {
                // Test-wise requires: Exam Category (mandatory), Exam Name (mandatory)
                shouldLoadData = examCategoryId.HasValue && examId.HasValue;
            }
            else if (filterMode == "studentwise")
            {
                // Student-wise requires: Student ID (mandatory), Exam Category (mandatory)
                shouldLoadData = studentId.HasValue && examCategoryId.HasValue;
            }
            else
            {
                // Default/legacy mode - just needs exam category
                shouldLoadData = examCategoryId.HasValue;
            }
            
            if (shouldLoadData)
            {
                var query = _context.ExamMarks
                    .Include(em => em.Student)
                    .Include(em => em.Exam)
                        .ThenInclude(e => e.ExamCategory)
                    .Include(em => em.Subject)
                    .Include(em => em.Class)
                    .Include(em => em.Section)
                    .Include(em => em.Campus)
                    .Where(em => em.IsActive && em.Exam.ExamCategoryId == examCategoryId.Value);
                
                if(usercampusId != null)
                {
                    query = query.Where(em => em.CampusId == usercampusId);
                }
                
                if (examId.HasValue) query = query.Where(em => em.ExamId == examId.Value);
                if (classId.HasValue) query = query.Where(em => em.ClassId == classId.Value);
                if (sectionId.HasValue) query = query.Where(em => em.SectionId == sectionId.Value);
                if (subjectId.HasValue) query = query.Where(em => em.SubjectId == subjectId.Value);
                // Filter by academic year if specified and not 0
                if (academicYear.HasValue && academicYear.Value != 0)
                {
                    query = query.Where(em => em.AcademicYear == academicYear.Value);
                }

                viewModel.ExamMarksList = await query.OrderBy(em => em.ExamId).ToListAsync();

                // Calculate statistics
                if (viewModel.ExamMarksList.Any())
                {
                    // Calculate unique student count to avoid duplication when multiple subjects/sections
                    var uniqueStudentIds = viewModel.ExamMarksList.Select(em => em.StudentId).Distinct().ToList();
                    viewModel.TotalStudents = uniqueStudentIds.Count;
                    
                    viewModel.AverageMarks = Math.Round(viewModel.ExamMarksList.Average(em => em.ObtainedMarks), 2);
                    viewModel.AveragePercentage = Math.Round(viewModel.ExamMarksList.Average(em => em.Percentage), 2);
                    viewModel.HighestMarks = viewModel.ExamMarksList.Max(em => em.ObtainedMarks);
                    viewModel.LowestMarks = viewModel.ExamMarksList.Min(em => em.ObtainedMarks);
                    
                    // Calculate pass/fail based on unique students
                    // A student passes if they pass in all their subjects
                    var passedStudentIds = viewModel.ExamMarksList
                        .GroupBy(em => em.StudentId)
                        .Where(g => g.All(em => em.Status != "Fail"))
                        .Select(g => g.Key)
                        .Distinct()
                        .Count();
                    
                    viewModel.PassedStudents = passedStudentIds;
                    viewModel.FailedStudents = viewModel.TotalStudents - viewModel.PassedStudents;
                    viewModel.PassPercentage = viewModel.TotalStudents > 0 ?
                        Math.Round((decimal)viewModel.PassedStudents / viewModel.TotalStudents * 100, 2) : 0;
                }
            }

            return View(viewModel);
        }

        // GET: Edit specific exam marks
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            var examMarks = await _context.ExamMarks
                .Include(em => em.Student)
                .Include(em => em.Exam)
                .Include(em => em.Subject)
                .Include(em => em.Class)
                .Include(em => em.Section)
                .FirstOrDefaultAsync(em => em.Id == id) ;

            if (examMarks == null)
            {
                return NotFound();
            }

            return View(examMarks);
        }

        // POST: Edit exam marks
        [HttpPost]
        public async Task<IActionResult> Edit(ExamMarks examMarks)
        {
            ModelState.Remove("Exam");
            ModelState.Remove("Class");
            ModelState.Remove("Grade");
            ModelState.Remove("Status");
            ModelState.Remove("Section");
            ModelState.Remove("Student");
            ModelState.Remove("Subject");
            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                var existingexamMarks = await _context.ExamMarks.FindAsync(examMarks.Id);
                if (existingexamMarks == null)
                {
                    return NotFound();
                }
                existingexamMarks.CalculateStatusAndGrade();
                existingexamMarks.ModifiedDate = DateTime.Now;
                existingexamMarks.ModifiedBy = User.Identity?.Name ?? "System";
                existingexamMarks.TotalMarks = examMarks.TotalMarks;
                existingexamMarks.ObtainedMarks = examMarks.ObtainedMarks;
                existingexamMarks.PassingMarks = examMarks.PassingMarks;
                existingexamMarks.Status = examMarks.Status;
                existingexamMarks.Grade = examMarks.Grade;
                existingexamMarks.Percentage = examMarks.Percentage;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Exam marks updated successfully!";
                return RedirectToAction(nameof(Analysis));
            }

            return View(examMarks);
        }

        // GET: Charts data for analysis
        [HttpGet]
        public async Task<JsonResult> GetChartsData(int? examId, int? classId, int? sectionId, int? subjectId)
        {
            var query = _context.ExamMarks.Where(em => em.IsActive);
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            if (examId.HasValue) query = query.Where(em => em.ExamId == examId.Value && em.CampusId == campusId);
            if (classId.HasValue) query = query.Where(em => em.ClassId == classId.Value && em.CampusId == campusId);
            if (sectionId.HasValue) query = query.Where(em => em.SectionId == sectionId.Value && em.CampusId == campusId);
            if (subjectId.HasValue) query = query.Where(em => em.SubjectId == subjectId.Value && em.CampusId == campusId);

            var examMarks = await query.ToListAsync();

            var gradeDistribution = examMarks
                .GroupBy(em => em.Grade)
                .Select(g => new { Grade = g.Key, Count = g.Count() })
                .OrderBy(x => x.Grade)
                .ToList();

            var statusDistribution = examMarks
                .GroupBy(em => em.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var marksRanges = new[]
            {
                new { Range = "0-40", Count = examMarks.Count(em => em.Percentage < 40) },
                new { Range = "40-50", Count = examMarks.Count(em => em.Percentage >= 40 && em.Percentage < 50) },
                new { Range = "50-60", Count = examMarks.Count(em => em.Percentage >= 50 && em.Percentage < 60) },
                new { Range = "60-70", Count = examMarks.Count(em => em.Percentage >= 60 && em.Percentage < 70) },
                new { Range = "70-80", Count = examMarks.Count(em => em.Percentage >= 70 && em.Percentage < 80) },
                new { Range = "80-90", Count = examMarks.Count(em => em.Percentage >= 80 && em.Percentage < 90) },
                new { Range = "90-100", Count = examMarks.Count(em => em.Percentage >= 90) }
            };

            return Json(new
            {
                gradeDistribution,
                statusDistribution,
                marksRanges
            });
        }

        // GET: Get analysis data for dashboard charts
        [HttpGet]
        public async Task<JsonResult> GetAnalysisChartsData(int? examCategoryId, int? examId, int? campusId, int? classId, int? sectionId, int? subjectId, int? academicYear, string filterMode)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isOwner = userRoles.Contains("Owner");

            // Handle Campus Logic: If 0 is passed, treat it as null (All Campuses) unless restricted by user role
            var requestCampusId = (campusId.HasValue && campusId.Value != 0) ? campusId : null;
            var usercampusId = requestCampusId ?? currentUser.CampusId;

            if (!isOwner && currentUser.CampusId != null)
            {
                usercampusId = currentUser.CampusId;
            }

            // Helper flags: True only if the ID is present AND not zero
            bool hasExamFilter = examId.HasValue && examId.Value != 0;
            bool hasClassFilter = classId.HasValue && classId.Value != 0;
            bool hasSectionFilter = sectionId.HasValue && sectionId.Value != 0;
            bool hasSubjectFilter = subjectId.HasValue && subjectId.Value != 0;
            bool hasCampusFilter = usercampusId.HasValue && usercampusId.Value != 0;

            // Base query - must have exam category
            if (!examCategoryId.HasValue || examCategoryId == 0)
            {
                return Json(new { success = false, message = "Exam category is required" });
            }

            var query = _context.ExamMarks
                .Include(em => em.Exam)
                    .ThenInclude(e => e.ExamCategory)
                .Include(em => em.Subject)
                .Include(em => em.Class)
                .Include(em => em.Section)
                .Include(em => em.Campus)
                .Where(em => em.IsActive && em.Exam.ExamCategoryId == examCategoryId.Value);

            // Apply Filters using the helper flags (checks for != 0)
            if (hasCampusFilter) query = query.Where(em => em.CampusId == usercampusId);
            if (hasExamFilter) query = query.Where(em => em.ExamId == examId.Value);
            if (hasClassFilter) query = query.Where(em => em.ClassId == classId.Value);
            if (hasSectionFilter) query = query.Where(em => em.SectionId == sectionId.Value);
            if (hasSubjectFilter) query = query.Where(em => em.SubjectId == subjectId.Value);
            // Filter by academic year if specified and not 0
            if (academicYear.HasValue && academicYear.Value != 0)
            {
                query = query.Where(em => em.AcademicYear == academicYear.Value);
            }

            var examMarks = await query.ToListAsync();

            // Pass/Fail distribution
            var passFailData = new
            {
                Passed = examMarks.Count(em => em.Status != "Fail"),
                Failed = examMarks.Count(em => em.Status == "Fail")
            };

            // Marks range distribution
            var marksRanges = new[]
            {
        new { Range = "0-40", Count = examMarks.Count(em => em.Percentage < 40) },
        new { Range = "40-50", Count = examMarks.Count(em => em.Percentage >= 40 && em.Percentage < 50) },
        new { Range = "50-60", Count = examMarks.Count(em => em.Percentage >= 50 && em.Percentage < 60) },
        new { Range = "60-70", Count = examMarks.Count(em => em.Percentage >= 60 && em.Percentage < 70) },
        new { Range = "70-80", Count = examMarks.Count(em => em.Percentage >= 70 && em.Percentage < 80) },
        new { Range = "80-90", Count = examMarks.Count(em => em.Percentage >= 80 && em.Percentage < 90) },
        new { Range = "90-100", Count = examMarks.Count(em => em.Percentage >= 90) }
    };

            // Mode-specific data
            object subjectWiseSectionData = null;
            object sectionWiseData = null;
            object classSectionWiseData = null;
            object subjectWiseData = null;
            object classWiseData = null;
            object campusWiseData = null;

            if (filterMode == "classwise")
            {
                // Subject-wise performance with section breakdown
                // Logic: Generate this if NO specific Subject is selected (ID is 0 or null)
                if (!hasSubjectFilter)
                {
                    subjectWiseSectionData = examMarks
                        .Where(em => em.Subject != null && em.Subject.Name != null &&
                                     em.Section != null && em.Section.Name != null)
                        .GroupBy(em => new { em.Subject.Name, SectionName = em.Section.Name })
                        .Select(g => new
                        {
                            Subject = g.Key.Name,
                            Section = g.Key.SectionName,
                            AveragePercentage = Math.Round(g.Average(em => em.Percentage), 2),
                            PassRate = g.Count() > 0 ? Math.Round((decimal)g.Count(em => em.Status != "Fail") / g.Count() * 100, 2) : 0
                        })
                        .OrderBy(x => x.Subject)
                        .ThenBy(x => x.Section)
                        .ToList();
                }

                // Section-wise performance
                // Logic: Generate this if NO specific Section is selected (ID is 0 or null)
                if (!hasSectionFilter)
                {
                    sectionWiseData = examMarks
                        .Where(em => em.Section != null && em.Section.Name != null)
                        .GroupBy(em => em.Section.Name)
                        .Select(g => new
                        {
                            Section = g.Key,
                            AveragePercentage = Math.Round(g.Average(em => em.Percentage), 2),
                            PassRate = g.Count() > 0 ? Math.Round((decimal)g.Count(em => em.Status != "Fail") / g.Count() * 100, 2) : 0,
                            TotalStudents = g.Select(em => em.StudentId).Distinct().Count()
                        })
                        .OrderBy(x => x.Section)
                        .ToList();
                }
            }
            else if (filterMode == "testwise")
            {
                // Class/Section-wise performance
                // Logic: Generate if BOTH Class AND Section are NOT selected together (show broader view)
                // This ensures we always see the graph unless we've drilled down to a specific class-section combination
                if (!(hasClassFilter && hasSectionFilter))
                {
                    classSectionWiseData = examMarks
                        .Where(em => em.Class != null && em.Class.Name != null &&
                                     em.Section != null && em.Section.Name != null)
                        .GroupBy(em => new { ClassName = em.Class.Name, SectionName = em.Section.Name, ClassId = em.ClassId })
                        .Select(g => new
                        {
                            ClassSection = $"{g.Key.ClassName} - {g.Key.SectionName}",
                            ClassName = g.Key.ClassName,
                            SectionName = g.Key.SectionName,
                            ClassId = g.Key.ClassId,
                            AveragePercentage = Math.Round(g.Average(em => em.Percentage), 2),
                            PassRate = g.Count() > 0 ? Math.Round((decimal)g.Count(em => em.Status != "Fail") / g.Count() * 100, 2) : 0,
                            TotalStudents = g.Select(em => em.StudentId).Distinct().Count()
                        })
                        .OrderBy(x => x.ClassName)
                        .ThenBy(x => x.SectionName)
                        .ToList();
                }
            }
            else
            {
                // Default/Legacy mode charts

                // Subject-wise performance: Generate if NO specific Subject is selected
                subjectWiseData = !hasSubjectFilter
                    ? examMarks
                        .Where(em => em.Subject != null && em.Subject.Name != null)
                        .GroupBy(em => em.Subject.Name)
                        .Select(g => new
                        {
                            Subject = g.Key,
                            AveragePercentage = Math.Round(g.Average(em => em.Percentage), 2),
                            PassRate = g.Count() > 0 ? Math.Round((decimal)g.Count(em => em.Status != "Fail") / g.Count() * 100, 2) : 0
                        })
                        .OrderBy(x => x.Subject)
                        .ToList()
                    : null;

                // Class-wise performance: Generate if NO specific Class is selected
                classWiseData = !hasClassFilter
                    ? examMarks
                        .Where(em => em.Class != null && em.Class.Name != null)
                        .GroupBy(em => em.Class.Name)
                        .Select(g => new
                        {
                            Class = g.Key,
                            AveragePercentage = Math.Round(g.Average(em => em.Percentage), 2),
                            PassRate = g.Count() > 0 ? Math.Round((decimal)g.Count(em => em.Status != "Fail") / g.Count() * 100, 2) : 0
                        })
                        .OrderBy(x => x.Class)
                        .ToList()
                    : null;

                // Campus-wise comparison: Generate if NO specific Campus is selected (and user is Owner)
                campusWiseData = isOwner && !hasCampusFilter
                    ? examMarks
                        .Where(em => em.Campus != null && em.Campus.Name != null)
                        .GroupBy(em => em.Campus.Name)
                        .Select(g => new
                        {
                            Campus = g.Key,
                            AveragePercentage = Math.Round(g.Average(em => em.Percentage), 2),
                            PassRate = g.Count() > 0 ? Math.Round((decimal)g.Count(em => em.Status != "Fail") / g.Count() * 100, 2) : 0,
                            TotalStudents = g.Select(em => em.StudentId).Distinct().Count()
                        })
                        .OrderBy(x => x.Campus)
                        .ToList()
                    : null;
            }

            return Json(new
            {
                success = true,
                passFailData,
                marksRanges,
                subjectWiseSectionData,
                sectionWiseData,
                classSectionWiseData,
                subjectWiseData,
                classWiseData,
                campusWiseData
            });
        }
        // POST: Recalculate marks for all students in a date sheet
        [HttpPost]
        public async Task<JsonResult> RecalculateMarksForDateSheet(int examDateSheetId)
        {
            try
            {
                var dateSheet = await _context.ExamDateSheets
                    .Include(eds => eds.ClassSections)
                    .FirstOrDefaultAsync(eds => eds.Id == examDateSheetId);
                
                if (dateSheet == null)
                    return Json(new { success = false, message = "Date sheet not found" });
                
                // Get all exam marks affected by this date sheet
                var affectedMarks = await _context.ExamMarks
                    .Where(em => em.ExamId == dateSheet.ExamId && 
                                em.SubjectId == dateSheet.SubjectId &&
                                em.CampusId == dateSheet.CampusId &&
                                dateSheet.ClassSections.Any(cs => cs.ClassId == em.ClassId && cs.SectionId == em.SectionId))
                    .ToListAsync();
                
                // Recalculate each
                foreach (var mark in affectedMarks)
                {
                    mark.TotalMarks = dateSheet.TotalMarks;
                    mark.PassingMarks = dateSheet.PassingMarks;
                    mark.CalculateStatusAndGrade();
                    mark.ModifiedDate = DateTime.Now;
                    mark.ModifiedBy = User.Identity?.Name ?? "System";
                }
                
                _context.ExamMarks.UpdateRange(affectedMarks);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, updatedCount = affectedMarks.Count, message = $"Successfully recalculated {affectedMarks.Count} student marks" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // GET: Search Students
        [HttpGet]
        public async Task<JsonResult> SearchStudents(string query)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var campusId = currentUser.CampusId;
                
                var students = await _context.Students
                    .Where(s => s.StudentName.Contains(query) && !s.HasLeft && s.CampusId == campusId)
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .Take(10)
                    .Select(s => new
                    {
                        id = s.Id,
                        studentName = s.StudentName,
                        className = s.ClassObj.Name,
                        sectionName = s.SectionObj.Name
                    })
                    .ToListAsync();
                
                return Json(students);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // GET: Get Student Analysis Data
        [HttpGet]
        public async Task<JsonResult> GetStudentAnalysisData(int studentId, int examCategoryId, int? academicYear)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var campusId = currentUser.CampusId;
                
                // Get student info - DO NOT restrict to current class/section
                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .FirstOrDefaultAsync(s => s.Id == studentId && s.CampusId == campusId);
                
                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found or access denied" });
                }
                
                // Get all exams in this category
                var exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == examCategoryId && e.IsActive)
                    .OrderBy(e => e.Id)
                    .ToListAsync();
                
                if (!exams.Any())
                {
                    return Json(new { success = false, message = "No exams found for this category" });
                }
                
                // Get all exam marks for this student in this category - ONLY FOR CURRENT CLASS
                var query = _context.ExamMarks
                    .Include(em => em.Subject)
                    .Include(em => em.Exam)
                    .Include(em => em.Class)
                    .Include(em => em.Section)
                    .Where(em => em.StudentId == studentId && 
                                em.Exam.ExamCategoryId == examCategoryId &&
                                em.CampusId == campusId &&
                                em.ClassId == student.Class && // Filter to current class only
                                em.IsActive);
                
                // Filter by academic year if specified
                if (academicYear.HasValue && academicYear.Value > 0)
                {
                    query = query.Where(em => em.AcademicYear == academicYear.Value);
                }
                
                var examMarks = await query.ToListAsync();
                
                if (!examMarks.Any())
                {
                    return Json(new { success = false, message = "No exam results found for this student" });
                }
                
                // Get latest exam (most recent)
                var latestExamId = examMarks.Max(em => em.ExamId);
                var latestExamMarks = examMarks.Where(em => em.ExamId == latestExamId).ToList();
                
                var latestExam = new
                {
                    examName = latestExamMarks.First().Exam.Name,
                    subjects = latestExamMarks.Select(em => new
                    {
                        subjectName = em.Subject.Name,
                        totalMarks = em.TotalMarks,
                        obtainedMarks = em.ObtainedMarks,
                        percentage = em.Percentage,
                        grade = em.Grade
                    }).ToList()
                };
                
                // Get all subjects for this student
                var allSubjects = examMarks.Select(em => new { em.SubjectId, em.Subject.Name })
                    .Distinct()
                    .OrderBy(s => s.Name)
                    .ToList();
                
                // Build history data
                var history = new
                {
                    exams = exams.Select(e => new
                    {
                        examId = e.Id,
                        examName = e.Name
                    }).ToList(),
                    subjects = allSubjects.Select(subj => new
                    {
                        subjectId = subj.SubjectId,
                        subjectName = subj.Name,
                        marks = exams.Select(exam => {
                            var mark = examMarks.FirstOrDefault(em => 
                                em.ExamId == exam.Id && em.SubjectId == subj.SubjectId);
                            return mark != null ? new
                            {
                                examId = exam.Id,
                                totalMarks = mark.TotalMarks,
                                obtainedMarks = mark.ObtainedMarks
                            } : null;
                        }).Where(m => m != null).ToList()
                    }).ToList()
                };
                
                // Build growth data (overall percentage for each exam)
                var growth = new
                {
                    exams = exams.Select(exam => {
                        var examMarksForExam = examMarks.Where(em => em.ExamId == exam.Id).ToList();
                        if (examMarksForExam.Any())
                        {
                            // Use aggregate instead of Sum to avoid potential overflow
                            var totalMarks = examMarksForExam.Aggregate(0m, (acc, em) => acc + em.TotalMarks);
                            var obtainedMarks = examMarksForExam.Aggregate(0m, (acc, em) => acc + em.ObtainedMarks);
                            var percentage = totalMarks > 0 ? Math.Round((obtainedMarks / totalMarks) * 100, 2) : 0;
                            
                            return new
                            {
                                examName = exam.Name,
                                percentage = percentage
                            };
                        }
                        return null;
                    }).Where(e => e != null).ToList()
                };
                
                return Json(new
                {
                    success = true,
                    studentName = student.StudentName,
                    latestExam = latestExam,
                    history = history,
                    growth = growth
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class SaveExamMarksRequest
    {
        public int ExamId { get; set; }
        public int ClassId { get; set; }
        public int CampusId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime ExamDate { get; set; }
        public List<StudentMarksEntry> StudentMarks { get; set; } = new List<StudentMarksEntry>();
    }
}