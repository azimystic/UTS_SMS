using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;
using SMS.ViewModels;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ReportService reportService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _reportService = reportService;
            _logger = logger;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        #region Award Sheet

        // GET: Reports/AwardSheet
        public async Task<IActionResult> AwardSheet()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new AwardSheetViewModel
            {
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && (!campusId.HasValue || ec.CampusId == null || ec.CampusId == campusId.Value))
                    .OrderBy(ec => ec.Name)
                    .ToListAsync(),

                Classes = await _context.Classes
                    .Where(c => c.IsActive && (!campusId.HasValue || c.CampusId == campusId.Value))
                    .OrderBy(c => c.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Reports/GenerateAwardSheet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAwardSheet(AwardSheetViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid input data. Please check all required fields.";
                    return RedirectToAction(nameof(AwardSheet));
                }

                // Fetch exam details
                var exam = await _context.Exams
                    .Include(e => e.ExamCategory)
                    .FirstOrDefaultAsync(e => e.Id == model.ExamId);

                var classObj = await _context.Classes.FindAsync(model.ClassId);
                var section = await _context.ClassSections.FindAsync(model.SectionId);
                var subject = await _context.Subjects.FindAsync(model.SubjectId);

                if (exam == null || classObj == null || section == null || subject == null)
                {
                    TempData["Error"] = "Invalid selection. Please verify all fields.";
                    return RedirectToAction(nameof(AwardSheet));
                }

                // Determine academic year
                int targetAcademicYear = 0;
                if (!string.IsNullOrEmpty(classObj.CurrentAcademicYear))
                {
                    var yearParts = classObj.CurrentAcademicYear.Split('-');
                    if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                    {
                        targetAcademicYear = parsedYear;
                    }
                }

                // ✅ CHECK IF SUBJECT HAS DATESHEET DEFINED
                var examDateSheet = await _context.ExamDateSheets
                    .Where(eds => eds.ExamId == model.ExamId && 
                                  eds.SubjectId == model.SubjectId && 
                                  eds.CampusId == classObj.CampusId && 
                                  eds.IsActive &&
                                  (targetAcademicYear == 0 || eds.AcademicYear == targetAcademicYear))
                    .Include(eds => eds.ClassSections)
                    .FirstOrDefaultAsync(eds => eds.ClassSections.Any(cs => cs.ClassId == model.ClassId && cs.SectionId == model.SectionId));

                if (examDateSheet == null)
                {
                    TempData["Error"] = $"This exam/subject is not defined in the Date Sheet for the selected class/section. Please add it to the Exam Date Sheet first.";
                    return RedirectToAction(nameof(AwardSheet));
                }

                // Fetch students in the class/section - SORTED BY ROLL NUMBER (nulls at end)
                var students = await _context.Students
                    .Where(s => s.Class == model.ClassId && s.Section == model.SectionId && !s.HasLeft)
                    .Include(s => s.SubjectsGrouping)
                        .ThenInclude(sg => sg.SubjectsGroupingDetails)
                            .ThenInclude(sgd => sgd.Subject)
                    .OrderBy(s => s.RollNumber == null ? 1 : 0)  // Nulls at end
                    .ThenBy(s => s.RollNumber)                    // Then sort by roll number
                    .ThenBy(s => s.StudentName)                   // Finally by name for nulls
                    .ToListAsync();

                // Fetch existing marks for this exam
                var examMarks = await _context.ExamMarks
                    .Where(em => em.ExamId == model.ExamId && 
                                 em.SubjectId == model.SubjectId &&
                                 em.ClassId == model.ClassId && 
                                 em.SectionId == model.SectionId &&
                                 (targetAcademicYear == 0 || em.AcademicYear == targetAcademicYear))
                    .ToListAsync();

                // Build common placeholders (header data)
                var commonPlaceholders = new Dictionary<string, string>
                {
                    { "exam_name", exam.Name },
                    { "exam_category", exam.ExamCategory.Name },
                    { "class_name", classObj.Name },
                    { "section_name", section.Name },
                    { "subject_name", subject.Name },
                    { "total_marks", examDateSheet.TotalMarks.ToString() },
                    { "passing_marks", examDateSheet.PassingMarks.ToString() },
                    { "date", DateTime.Now.ToString("dd/MM/yyyy") },
                    { "count", students.Count.ToString() }
                };

                // Build row data - one dictionary per student
                var rowData = new List<Dictionary<string, string>>();
                int serialNo = 1;
                
                foreach (var student in students)
                {
                    var marks = examMarks.FirstOrDefault(em => em.StudentId == student.Id);
                    
                    // Check if student is enrolled in this subject
                    var isEnrolledInSubject = student.SubjectsGrouping?.SubjectsGroupingDetails?
                        .Any(sgd => sgd.SubjectId == model.SubjectId && sgd.IsActive) ?? false;
                    
                    var studentRow = new Dictionary<string, string>
                    {
                        { "serial_no", serialNo.ToString() },
                        { "student_name", student.StudentName },
                        { "student_roll", student.RollNumber ?? "" },
                        // Show blank if no marks, N/A if not enrolled
                        { "student_obtained", !isEnrolledInSubject ? "N/A" : (marks != null ? marks.ObtainedMarks.ToString() : "") },
                        { "student_grade", !isEnrolledInSubject ? "N/A" : (marks?.Grade ?? "") },
                        { "student_remarks", !isEnrolledInSubject ? "Not Enrolled" : (marks?.Remarks ?? "") }
                    };
                    
                    rowData.Add(studentRow);
                    serialNo++;
                }

                // Define template placeholders that should be in the Word template row
                var templateRowPlaceholders = new List<string>
                {
                    "serial_no",
                    "student_name",
                    "student_roll",
                    "student_obtained",
                    "student_grade",
                    "student_remarks"
                };

                // Generate PDF with dynamic rows
                var pdfBytes = await _reportService.GeneratePdfWithDynamicRows(
                    "AwardSheet.docx", 
                    commonPlaceholders, 
                    rowData, 
                    templateRowPlaceholders);

                return File(pdfBytes, "application/pdf", $"AwardSheet_{classObj.Name}_{section.Name}_{subject.Name}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating award sheet");
                TempData["Error"] = "Error generating report. Please ensure LibreOffice is installed and the template exists.";
                return RedirectToAction(nameof(AwardSheet));
            }
        }

        #endregion

        #region Class Exam Report (Broadsheet)

        // GET: Reports/ClassExamReport
        public async Task<IActionResult> ClassExamReport()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new ClassExamReportViewModel
            {
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && (!campusId.HasValue || ec.CampusId == null || ec.CampusId == campusId.Value))
                    .OrderBy(ec => ec.Name)
                    .ToListAsync(),

                Classes = await _context.Classes
                    .Where(c => c.IsActive && (!campusId.HasValue || c.CampusId == campusId.Value))
                    .OrderBy(c => c.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Reports/GenerateClassExamReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateClassExamReport(ClassExamReportViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid input data. Please check all required fields.";
                    return RedirectToAction(nameof(ClassExamReport));
                }

                var examCategory = await _context.ExamCategories.FindAsync(model.ExamCategoryId);
                var classObj = await _context.Classes.FindAsync(model.ClassId);
                var section = await _context.ClassSections.FindAsync(model.SectionId);

                if (examCategory == null || classObj == null || section == null)
                {
                    TempData["Error"] = "Invalid selection. Please verify all fields.";
                    return RedirectToAction(nameof(ClassExamReport));
                }

                // Determine academic year
                int targetAcademicYear = 0;
                if (!string.IsNullOrEmpty(classObj.CurrentAcademicYear))
                {
                    var yearParts = classObj.CurrentAcademicYear.Split('-');
                    if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                    {
                        targetAcademicYear = parsedYear;
                    }
                }

                // Get all exams in this category
                var exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == model.ExamCategoryId && e.IsActive)
                    .OrderBy(e => e.Id)
                    .ToListAsync();

                if (!exams.Any())
                {
                    TempData["Error"] = "No exams found in this category.";
                    return RedirectToAction(nameof(ClassExamReport));
                }

                // Get all exam IDs
                var examIds = exams.Select(e => e.Id).ToList();

                // Get all exam date sheets for these exams and class/section
                var examDateSheets = await _context.ExamDateSheets
                    .Where(eds => examIds.Contains(eds.ExamId) && 
                                  eds.IsActive &&
                                  (targetAcademicYear == 0 || eds.AcademicYear == targetAcademicYear))
                    .Include(eds => eds.Subject)
                    .Include(eds => eds.ClassSections)
                    .Where(eds => eds.ClassSections.Any(cs => cs.ClassId == model.ClassId && cs.SectionId == model.SectionId))
                    .ToListAsync();

                if (!examDateSheets.Any())
                {
                    TempData["Error"] = "No exam date sheets found for this class/section in the selected exam category.";
                    return RedirectToAction(nameof(ClassExamReport));
                }

                // Build exam structure with subjects
                var examWithSubjects = new List<Services.ExamWithSubjects>();
                foreach (var exam in exams)
                {
                    var subjects = examDateSheets
                        .Where(eds => eds.ExamId == exam.Id)
                        .Select(eds => new Services.SubjectInfo
                        {
                            SubjectId = eds.SubjectId,
                            SubjectCode = eds.Subject.Code ?? eds.Subject.Name.Substring(0, Math.Min(3, eds.Subject.Name.Length)),
                            SubjectName = eds.Subject.Name
                        })
                        .OrderBy(s => s.SubjectName)
                        .ToList();

                    if (subjects.Any())
                    {
                        examWithSubjects.Add(new Services.ExamWithSubjects
                        {
                            ExamId = exam.Id,
                            ExamName = exam.Name,
                            Subjects = subjects
                        });
                    }
                }

                // Get all students in class/section
                var students = await _context.Students
                    .Where(s => s.Class == model.ClassId && s.Section == model.SectionId && !s.HasLeft)
                    .Include(s => s.SubjectsGrouping)
                        .ThenInclude(sg => sg.SubjectsGroupingDetails)
                    .OrderBy(s => s.RollNumber == null ? 1 : 0)
                    .ThenBy(s => s.RollNumber)
                    .ThenBy(s => s.StudentName)
                    .ToListAsync();

                if (!students.Any())
                {
                    TempData["Error"] = "No students found in this class/section.";
                    return RedirectToAction(nameof(ClassExamReport));
                }

                // Get all marks for these students in these exams
                var studentIds = students.Select(s => s.Id).ToList();
                var allMarks = await _context.ExamMarks
                    .Where(em => examIds.Contains(em.ExamId) &&
                                 studentIds.Contains(em.StudentId) &&
                                 em.ClassId == model.ClassId &&
                                 em.SectionId == model.SectionId &&
                                 (targetAcademicYear == 0 || em.AcademicYear == targetAcademicYear) &&
                                 em.IsActive)
                    .ToListAsync();

                // Build student data with rankings
                var studentDataList = new List<Services.StudentExamData>();

                foreach (var student in students)
                {
                    var studentMarks = allMarks.Where(m => m.StudentId == student.Id).ToList();
                    
                    // Get student's enrolled subjects
                    var enrolledSubjectIds = student.SubjectsGrouping?.SubjectsGroupingDetails?
                        .Where(sgd => sgd.IsActive)
                        .Select(sgd => sgd.SubjectId)
                        .ToList() ?? new List<int>();

                    var marksDataList = new List<Services.StudentMarksData>();
                    decimal totalObtained = 0;
                    decimal totalMax = 0;

                    // Process each exam and its subjects
                    foreach (var exam in examWithSubjects)
                    {
                        foreach (var subject in exam.Subjects)
                        {
                            // Check if student is enrolled in this subject
                            bool isEnrolled = enrolledSubjectIds.Contains(subject.SubjectId);
                            
                            var mark = studentMarks.FirstOrDefault(m => m.ExamId == exam.ExamId && m.SubjectId == subject.SubjectId);
                            
                            if (mark != null && isEnrolled)
                            {
                                marksDataList.Add(new Services.StudentMarksData
                                {
                                    ExamId = exam.ExamId,
                                    SubjectId = subject.SubjectId,
                                    HasMarks = true,
                                    ObtainedMarks = mark.ObtainedMarks,
                                    TotalMarks = mark.TotalMarks
                                });

                                totalObtained += mark.ObtainedMarks;
                                totalMax += mark.TotalMarks;
                            }
                            else
                            {
                                marksDataList.Add(new Services.StudentMarksData
                                {
                                    ExamId = exam.ExamId,
                                    SubjectId = subject.SubjectId,
                                    HasMarks = false,
                                    ObtainedMarks = 0,
                                    TotalMarks = 0
                                });
                            }
                        }
                    }

                    decimal percentage = totalMax > 0 ? Math.Round((totalObtained / totalMax) * 100, 2) : 0;

                    studentDataList.Add(new Services.StudentExamData
                    {
                        StudentId = student.Id,
                        StudentName = student.StudentName,

                        // Safely take the last 3 characters
                        RollNumber = student.RollNumber?.Length >= 3
        ? student.RollNumber[^3..]
        : student.RollNumber,

                        TotalObtained = totalObtained,
                        TotalMax = totalMax,
                        Percentage = percentage,
                        Marks = marksDataList
                    });
                }

                // Calculate rankings based on percentage
                var rankedStudents = studentDataList
                    .OrderByDescending(s => s.Percentage)
                    .ThenByDescending(s => s.TotalObtained)
                    .ToList();

                for (int i = 0; i < rankedStudents.Count; i++)
                {
                    rankedStudents[i].Rank = i + 1;
                }

                // Build header data
                var headerData = new Dictionary<string, string>
                {
                    { "exam_category", examCategory.Name },
                    { "class_name", classObj.Name },
                    { "section_name", section.Name },
                    { "date", DateTime.Now.ToString("dd/MM/yyyy") }
                };

                // Generate Excel and convert to PDF
                var pdfBytes = await _reportService.GenerateClassExamReportExcel(
                    "ClassExamReport.xlsx",
                    headerData,
                    examWithSubjects,
                    rankedStudents);

                return File(pdfBytes, 
                    "application/pdf",
                    $"ClassExamReport_{classObj.Name}_{section.Name}_{examCategory.Name}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating class exam report");
                TempData["Error"] = $"Error generating report: {ex.Message}";
                return RedirectToAction(nameof(ClassExamReport));
            }
        }

        #endregion

        #region Student Report Card

        // GET: Reports/StudentReportCard
        public async Task<IActionResult> StudentReportCard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new StudentReportCardViewModel
            {
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && (!campusId.HasValue || ec.CampusId == null || ec.CampusId == campusId.Value))
                    .OrderBy(ec => ec.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Reports/GenerateStudentReportCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateStudentReportCard(StudentReportCardViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid input data. Please check all required fields.";
                    return RedirectToAction(nameof(StudentReportCard));
                }

                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .Include(s => s.SubjectsGrouping)
                        .ThenInclude(sg => sg.SubjectsGroupingDetails)
                            .ThenInclude(sgd => sgd.Subject)
                    .FirstOrDefaultAsync(s => s.Id == model.StudentId);

                var exam = await _context.Exams
                    .Include(e => e.ExamCategory)
                    .FirstOrDefaultAsync(e => e.Id == model.ExamId);

                if (student == null || exam == null)
                {
                    TempData["Error"] = "Invalid selection. Please verify student and exam.";
                    return RedirectToAction(nameof(StudentReportCard));
                }

                // Get all subjects from student's subject grouping
                var enrolledSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                    .Where(sgd => sgd.IsActive)
                    .Select(sgd => sgd.Subject)
                    .OrderBy(s => s.Name)
                    .ToList() ?? new List<Subject>();

                if (!enrolledSubjects.Any())
                {
                    TempData["Error"] = "Student has no subjects assigned in their subject grouping.";
                    return RedirectToAction(nameof(StudentReportCard));
                }

                // Get student marks for this exam
                var examMarks = await _context.ExamMarks
                    .Where(em => em.StudentId == model.StudentId && em.ExamId == model.ExamId)
                    .ToListAsync();

                // Build subject row data
                var subjectRowData = new List<Dictionary<string, string>>();
                decimal totalMarks = 0;
                decimal obtainedMarks = 0;

                foreach (var subject in enrolledSubjects)
                {
                    var mark = examMarks.FirstOrDefault(m => m.SubjectId == subject.Id);

                    if (mark != null)
                    {
                        // Student has marks for this subject
                        subjectRowData.Add(new Dictionary<string, string>
                        {
                            { "subject_name", subject.Name },
                            { "subject_obtained", mark.ObtainedMarks.ToString() },
                            { "subject_total", mark.TotalMarks.ToString() },
                            { "subject_percentage", mark.Percentage.ToString("F2") },
                            { "subject_grade", mark.Grade ?? "" }
                        });

                        totalMarks += mark.TotalMarks;
                        obtainedMarks += mark.ObtainedMarks;
                    }
                    else
                    {
                        // Student is enrolled but has no marks - show N/A
                        subjectRowData.Add(new Dictionary<string, string>
                        {
                            { "subject_name", subject.Name },
                            { "subject_obtained", "N/A" },
                            { "subject_total", "N/A" },
                            { "subject_percentage", "N/A" },
                            { "subject_grade", "N/A" }
                        });
                    }
                }

                var percentage = totalMarks > 0 ? Math.Round((obtainedMarks / totalMarks) * 100, 2) : 0;

                // Calculate position
                var allStudentsMarks = await _context.ExamMarks
                    .Where(em => em.ExamId == model.ExamId &&
                                 em.ClassId == student.Class &&
                                 em.SectionId == student.Section)
                    .GroupBy(em => em.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        TotalObtained = g.Sum(m => m.ObtainedMarks)
                    })
                    .OrderByDescending(x => x.TotalObtained)
                    .ToListAsync();

                var position = allStudentsMarks.FindIndex(x => x.StudentId == student.Id) + 1;
                if (position == 0) position = allStudentsMarks.Count + 1; // If not in list, last position

                // Build common placeholders (header data)
                var commonPlaceholders = new Dictionary<string, string>
                {
                    { "student_name", student.StudentName },
                    { "father_name", student.FatherName },
                    { "roll_number", student.RollNumber ?? "" },
                    { "class_name", student.ClassObj.Name },
                    { "section_name", student.SectionObj.Name },
                    { "exam_name", exam.Name },
                    { "total_marks", totalMarks.ToString() },
                    { "obtained_marks", obtainedMarks.ToString() },
                    { "percentage", percentage.ToString("F2") },
                    { "position", position.ToString() },
                    { "date", DateTime.Now.ToString("dd/MM/yyyy") }
                };

                // Define template placeholders for subject rows
                var templateRowPlaceholders = new List<string>
                {
                    "subject_name",
                    "subject_obtained",
                    "subject_total",
                    "subject_percentage",
                    "subject_grade"
                };

                // Generate PDF with dynamic subject rows
                var pdfBytes = await _reportService.GeneratePdfWithDynamicRows(
                    "StudentReportCard.docx",
                    commonPlaceholders,
                    subjectRowData,
                    templateRowPlaceholders);

                return File(pdfBytes, "application/pdf", $"ReportCard_{student.StudentName}_{exam.Name}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating student report card");
                TempData["Error"] = "Error generating report. Please ensure LibreOffice is installed and the template exists.";
                return RedirectToAction(nameof(StudentReportCard));
            }
        }

        #endregion

        #region Report Card with History

        // GET: Reports/ReportCardHistory
        public async Task<IActionResult> ReportCardHistory()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new ReportCardHistoryViewModel
            {
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && (!campusId.HasValue || ec.CampusId == null || ec.CampusId == campusId.Value))
                    .OrderBy(ec => ec.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Reports/GenerateReportCardHistory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReportCardHistory(ReportCardHistoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid input data. Please check all required fields.";
                    return RedirectToAction(nameof(ReportCardHistory));
                }

                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .FirstOrDefaultAsync(s => s.Id == model.StudentId);

                var focusExam = await _context.Exams
                    .Include(e => e.ExamCategory)
                    .FirstOrDefaultAsync(e => e.Id == model.FocusExamId);

                if (student == null || focusExam == null)
                {
                    TempData["Error"] = "Invalid selection. Please verify student and exam.";
                    return RedirectToAction(nameof(ReportCardHistory));
                }

                // Get all exams in the same category
                var categoryExams = await _context.Exams
                    .Where(e => e.ExamCategoryId == model.ExamCategoryId && e.IsActive)
                    .OrderByDescending(e => e.Id)
                    .ToListAsync();

                // Build placeholders
                var placeholders = new Dictionary<string, string>
                {
                    { "student_name", student.StudentName },
                    { "father_name", student.FatherName },
                    { "roll_number", student.RollNumber ?? "" },
                    { "class_name", student.ClassObj.Name },
                    { "section_name", student.SectionObj.Name },
                    { "exam_category", focusExam.ExamCategory.Name },
                    { "date", DateTime.Now.ToString("dd/MM/yyyy") }
                };

                // Add focus exam data
                var focusMarks = await _context.ExamMarks
                    .Include(em => em.Subject)
                    .Where(em => em.StudentId == model.StudentId && em.ExamId == model.FocusExamId)
                    .ToListAsync();

                var focusTotalMarks = focusMarks.Sum(m => m.TotalMarks);
                var focusObtainedMarks = focusMarks.Sum(m => m.ObtainedMarks);
                var focusPercentage = focusTotalMarks > 0 ? Math.Round((focusObtainedMarks / focusTotalMarks) * 100, 2) : 0;

                placeholders["focus_exam_name"] = focusExam.Name;
                placeholders["focus_total_marks"] = focusTotalMarks.ToString();
                placeholders["focus_obtained_marks"] = focusObtainedMarks.ToString();
                placeholders["focus_percentage"] = focusPercentage.ToString("F2");

                // Add historical exams
                int historyIndex = 0;
                foreach (var exam in categoryExams.Where(e => e.Id != model.FocusExamId).Take(5))
                {
                    var examMarks = await _context.ExamMarks
                        .Where(em => em.StudentId == model.StudentId && em.ExamId == exam.Id)
                        .ToListAsync();

                    var totalMarks = examMarks.Sum(m => m.TotalMarks);
                    var obtainedMarks = examMarks.Sum(m => m.ObtainedMarks);
                    var percentage = totalMarks > 0 ? Math.Round((obtainedMarks / totalMarks) * 100, 2) : 0;

                    placeholders[$"history_{historyIndex + 1}_exam"] = exam.Name;
                    placeholders[$"history_{historyIndex + 1}_total"] = totalMarks.ToString();
                    placeholders[$"history_{historyIndex + 1}_obtained"] = obtainedMarks.ToString();
                    placeholders[$"history_{historyIndex + 1}_percentage"] = percentage.ToString("F2");

                    historyIndex++;
                }

                // Generate PDF
                var pdfBytes = await _reportService.GeneratePdfFromTemplate("ReportCardHistory.docx", placeholders);

                return File(pdfBytes, "application/pdf", $"ReportCardHistory_{student.StudentName}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report card with history");
                TempData["Error"] = "Error generating report. Please ensure LibreOffice is installed and the template exists.";
                return RedirectToAction(nameof(ReportCardHistory));
            }
        }

        #endregion

        #region Bulk Report Card

        // GET: Reports/BulkReportCard
        public async Task<IActionResult> BulkReportCard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new BulkReportCardViewModel
            {
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && (!campusId.HasValue || ec.CampusId == null || ec.CampusId == campusId.Value))
                    .OrderBy(ec => ec.Name)
                    .ToListAsync(),

                Classes = await _context.Classes
                    .Where(c => c.IsActive && (!campusId.HasValue || c.CampusId == campusId.Value))
                    .OrderBy(c => c.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Reports/GenerateBulkReportCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBulkReportCard(BulkReportCardViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid input data. Please check all required fields.";
                    return RedirectToAction(nameof(BulkReportCard));
                }

                var classObj = await _context.Classes.FindAsync(model.ClassId);
                var section = await _context.ClassSections.FindAsync(model.SectionId);
                var exam = await _context.Exams
                    .Include(e => e.ExamCategory)
                    .FirstOrDefaultAsync(e => e.Id == model.ExamId);

                if (classObj == null || section == null || exam == null)
                {
                    TempData["Error"] = "Invalid selection. Please verify class, section, and exam.";
                    return RedirectToAction(nameof(BulkReportCard));
                }

                // Get all students in the class/section with their subject groupings
                var students = await _context.Students
                    .Where(s => s.Class == model.ClassId && s.Section == model.SectionId && !s.HasLeft)
                    .Include(s => s.SubjectsGrouping)
                        .ThenInclude(sg => sg.SubjectsGroupingDetails)
                            .ThenInclude(sgd => sgd.Subject)
                    .OrderBy(s => s.StudentName)
                    .ToListAsync();

                // Get all marks for the exam in one query
                var allMarks = await _context.ExamMarks
                    .Include(em => em.Subject)
                    .Where(em => em.ExamId == model.ExamId &&
                                 em.ClassId == model.ClassId &&
                                 em.SectionId == model.SectionId)
                    .ToListAsync();

                // Calculate positions once for all students
                var studentRankings = allMarks
                    .GroupBy(em => em.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        TotalObtained = g.Sum(m => m.ObtainedMarks)
                    })
                    .OrderByDescending(x => x.TotalObtained)
                    .ToList();

                // Generate report data for each student using dynamic rows
                var studentReportDataList = new List<(Dictionary<string, string> commonPlaceholders, List<Dictionary<string, string>> subjectRows)>();

                foreach (var student in students)
                {
                    // Get enrolled subjects for this student
                    var enrolledSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                        .Where(sgd => sgd.IsActive)
                        .Select(sgd => sgd.Subject)
                        .OrderBy(s => s.Name)
                        .ToList() ?? new List<Subject>();

                    if (!enrolledSubjects.Any())
                    {
                        // Skip students with no subject grouping
                        continue;
                    }

                    var studentMarks = allMarks.Where(m => m.StudentId == student.Id).ToList();

                    // Build subject row data
                    var subjectRowData = new List<Dictionary<string, string>>();
                    decimal totalMarks = 0;
                    decimal obtainedMarks = 0;

                    foreach (var subject in enrolledSubjects)
                    {
                        var mark = studentMarks.FirstOrDefault(m => m.SubjectId == subject.Id);

                        if (mark != null)
                        {
                            // Student has marks for this subject
                            subjectRowData.Add(new Dictionary<string, string>
                            {
                                { "subject_name", subject.Name },
                                { "subject_obtained", mark.ObtainedMarks.ToString() },
                                { "subject_total", mark.TotalMarks.ToString() },
                                { "subject_percentage", mark.Percentage.ToString("F2") },
                                { "subject_grade", mark.Grade ?? "" }
                            });

                            totalMarks += mark.TotalMarks;
                            obtainedMarks += mark.ObtainedMarks;
                        }
                        else
                        {
                            // Student is enrolled but has no marks - show N/A
                            subjectRowData.Add(new Dictionary<string, string>
                            {
                                { "subject_name", subject.Name },
                                { "subject_obtained", "N/A" },
                                { "subject_total", "N/A" },
                                { "subject_percentage", "N/A" },
                                { "subject_grade", "N/A" }
                            });
                        }
                    }

                    var percentage = totalMarks > 0 ? Math.Round((obtainedMarks / totalMarks) * 100, 2) : 0;

                    // Get position from pre-calculated rankings
                    var position = studentRankings.FindIndex(x => x.StudentId == student.Id) + 1;
                    if (position == 0) position = studentRankings.Count + 1;

                    // Build common placeholders
                    var commonPlaceholders = new Dictionary<string, string>
                    {
                        { "student_name", student.StudentName },
                        { "father_name", student.FatherName },
                        { "roll_number", student.RollNumber ?? "" },
                        { "class_name", classObj.Name },
                        { "section_name", section.Name },
                        { "exam_name", exam.Name },
                        { "total_marks", totalMarks.ToString() },
                        { "obtained_marks", obtainedMarks.ToString() },
                        { "percentage", percentage.ToString("F2") },
                        { "position", position.ToString() },
                        { "date", DateTime.Now.ToString("dd/MM/yyyy") }
                    };

                    studentReportDataList.Add((commonPlaceholders, subjectRowData));
                }

                if (!studentReportDataList.Any())
                {
                    TempData["Error"] = "No students with valid subject groupings found.";
                    return RedirectToAction(nameof(BulkReportCard));
                }

                // Define template placeholders for subject rows
                var templateRowPlaceholders = new List<string>
                {
                    "subject_name",
                    "subject_obtained",
                    "subject_total",
                    "subject_percentage",
                    "subject_grade"
                };

                // Generate bulk PDFs with dynamic rows as single merged PDF
                var pdfBytes = await _reportService.GenerateBulkPdfsWithDynamicRows(
                    "StudentReportCard.docx",
                    studentReportDataList,
                    templateRowPlaceholders,
                    asZip: false);

                return File(pdfBytes, "application/pdf", $"ReportCards_{classObj.Name}_{section.Name}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bulk report cards");
                TempData["Error"] = "Error generating reports. Please ensure LibreOffice is installed and the template exists.";
                return RedirectToAction(nameof(BulkReportCard));
            }
        }

        #endregion

        #region Helper Methods - AJAX endpoints for cascading dropdowns

        [HttpGet]
        public async Task<IActionResult> GetSections(int classId)
        {
            var sections = await _context.ClassSections
                .Where(s => s.ClassId == classId && s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(sections);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjects(int classId)
        {
            // Get subjects that are assigned to this class through TeacherAssignment
            var subjects = await _context.TeacherAssignments
                .Where(ta => ta.ClassId == classId && ta.IsActive)
                .Select(ta => ta.Subject)
                .Distinct()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectsByClassSection(int classId, int sectionId)
        {
            // Get subjects that are assigned to this specific class and section
            var subjects = await _context.Subjects
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> GetExams(int examCategoryId)
        {
            var exams = await _context.Exams
                .Where(e => e.ExamCategoryId == examCategoryId && e.IsActive)
                .OrderBy(e => e.Name)
                .Select(e => new { id = e.Id, name = e.Name })
                .ToListAsync();

            return Json(exams);
        }

        [HttpPost]
        public async Task<IActionResult> SearchStudent(string searchTerm)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new { success = false, message = "Please enter search term" });
            }

            var students = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Where(s =>
                    (!campusId.HasValue || s.CampusId == campusId.Value) &&
                    !s.HasLeft &&
                    (s.StudentName.Contains(searchTerm) ||
                     s.FatherName.Contains(searchTerm) ||
                     s.StudentCNIC.Contains(searchTerm) ||
                     (s.RollNumber != null && s.RollNumber.Contains(searchTerm)))
                )
                .Select(s => new
                {
                    id = s.Id,
                    studentName = s.StudentName,
                    fatherName = s.FatherName,
                    className = s.ClassObj.Name,
                    sectionName = s.SectionObj.Name,
                    rollNumber = s.RollNumber
                })
                .Take(10)
                .ToListAsync();

            return Json(new { success = true, students = students });
        }

        #endregion
    }
}
