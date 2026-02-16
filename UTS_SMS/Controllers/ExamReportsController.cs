using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.ViewModels;
using System.Linq;

namespace SMS.Controllers
{
    public class ExamReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        // GET: ExamReports
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new ExamReportsIndexViewModel
            {
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive)
                    .OrderBy(ec => ec.Name)
                    .ToListAsync(),

                Classes = await _context.Classes
                    .Where(c => c.IsActive && (!campusId.HasValue || c.CampusId == campusId.Value))
                    .OrderBy(c => c.Name)
                    .ToListAsync(),

                Campus = campusId.HasValue
                    ? await _context.Campuses.FindAsync(campusId.Value)
                    : null
            };

            return View(viewModel);
        }


        // POST: Search Student
        [HttpPost]
        public async Task<IActionResult> SearchStudent(string searchTerm)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new { success = false, message = "Please enter search term" });
            }

            var studentsQuery = _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Where(s =>
                    (!campusId.HasValue || s.CampusId == campusId.Value) &&
                    (s.StudentName.Contains(searchTerm) ||
                     s.FatherName.Contains(searchTerm) ||
                     s.StudentCNIC.Contains(searchTerm))
                );

            var students = await studentsQuery
                .Select(s => new StudentSearchResult
                {
                    Id = s.Id,
                    StudentName = s.StudentName,
                    FatherName = s.FatherName,
                    ClassName = s.ClassObj.Name,
                    SectionName = s.SectionObj.Name,
                    StudentCNIC = s.StudentCNIC
                })
                .Take(10)
                .ToListAsync();

            return Json(new { success = true, students = students });
        }


        // GET: Student Exam Report
        public async Task<IActionResult> StudentExamReport(int studentId, int? examCategoryId = null)
        {
 
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                    .ThenInclude(sgd => sgd.Subject)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == studentId );

            if (student == null)
            {
                return NotFound();
            }

            var examCategories = await _context.ExamCategories
                .Where(ec => ec.IsActive)
                .ToListAsync();

            var viewModel = new StudentExamReportViewModel
            {
                Student = student,
                ExamCategories = examCategories,
                SelectedExamCategoryId = examCategoryId,
                Campus = student.Campus
            };

            if (examCategoryId.HasValue)
            {
                var exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == examCategoryId.Value && e.IsActive)
                    .ToListAsync();

                var studentSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                    .Where(sgd => sgd.IsActive)
                    .Select(sgd => sgd.Subject)
                    .ToList() ?? new List<Subject>();

                var examReports = new List<StudentExamCategoryReport>();

                foreach (var exam in exams)
                {
                    var examMarks = await _context.ExamMarks
                        .Include(em => em.Subject)
                        .Where(em => em.StudentId == studentId &&
                                   em.ExamId == exam.Id &&
                                   em.IsActive)
                        .ToListAsync();

                    var subjectMarks = new List<StudentSubjectMark>();
                    decimal totalObtained = 0;
                    decimal totalMarks = 0;

                    foreach (var subject in studentSubjects)
                    {
                        var mark = examMarks.FirstOrDefault(em => em.SubjectId == subject.Id);
                        if (mark != null)
                        {
                            subjectMarks.Add(new StudentSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = mark.TotalMarks,
                                ObtainedMarks = mark.ObtainedMarks,
                                Grade = mark.Grade,
                                Status = mark.Status,
                                HasEntry = true
                            });
                            totalObtained += mark.ObtainedMarks;
                            totalMarks += mark.TotalMarks;
                        }
                        else
                        {
                            // Default marks if no entry exists (you can adjust these values)
                            var defaultTotalMarks = 100m; // or get from exam configuration
                            subjectMarks.Add(new StudentSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = defaultTotalMarks,
                                ObtainedMarks = 0,
                                Grade = "F",
                                Status = "Absent",
                                HasEntry = false
                            });
                            totalMarks += defaultTotalMarks;
                        }
                    }

                    var percentage = totalMarks > 0 ? Math.Round((totalObtained / totalMarks) * 100, 2) : 0;

                    examReports.Add(new StudentExamCategoryReport
                    {
                        ExamName = exam.Name,
                        ExamId = exam.Id,
                        SubjectMarks = subjectMarks,
                        TotalObtained = totalObtained,
                        TotalMarks = totalMarks,
                        Percentage = percentage,
                        OverallGrade = CalculateOverallGrade(percentage),
                        OverallStatus = percentage >= 33 ? "Pass" : "Fail" // Adjust passing criteria as needed
                    });
                }

                viewModel.ExamReports = examReports;
            }

            return View(viewModel);
        }

        public async Task<IActionResult> PrintReport(int studentId, int? examCategoryId = null)
        {
 
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                    .ThenInclude(sgd => sgd.Subject)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == studentId );

            if (student == null)
            {
                return NotFound();
            }

            var examCategories = await _context.ExamCategories
                .Where(ec => ec.IsActive)
                .ToListAsync();

            var viewModel = new StudentExamReportViewModel
            {
                Student = student,
                ExamCategories = examCategories,
                SelectedExamCategoryId = examCategoryId,
                Campus = student.Campus
            };

            if (examCategoryId.HasValue)
            {
                var exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == examCategoryId.Value && e.IsActive)
                    .ToListAsync();

                var studentSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                    .Where(sgd => sgd.IsActive)
                    .Select(sgd => sgd.Subject)
                    .ToList() ?? new List<Subject>();

                var examReports = new List<StudentExamCategoryReport>();

                foreach (var exam in exams)
                {
                    var examMarks = await _context.ExamMarks
                        .Include(em => em.Subject)
                        .Where(em => em.StudentId == studentId &&
                                   em.ExamId == exam.Id &&
                                   em.IsActive)
                        .ToListAsync();

                    var subjectMarks = new List<StudentSubjectMark>();
                    decimal totalObtained = 0;
                    decimal totalMarks = 0;

                    foreach (var subject in studentSubjects)
                    {
                        var mark = examMarks.FirstOrDefault(em => em.SubjectId == subject.Id);
                        if (mark != null)
                        {
                            subjectMarks.Add(new StudentSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = mark.TotalMarks,
                                ObtainedMarks = mark.ObtainedMarks,
                                Grade = mark.Grade,
                                Status = mark.Status,
                                HasEntry = true
                            });
                            totalObtained += mark.ObtainedMarks;
                            totalMarks += mark.TotalMarks;
                        }
                        else
                        {
                            var defaultTotalMarks = 100m;
                            subjectMarks.Add(new StudentSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = defaultTotalMarks,
                                ObtainedMarks = 0,
                                Grade = "F",
                                Status = "Absent",
                                HasEntry = false
                            });
                            totalMarks += defaultTotalMarks;
                        }
                    }

                    var percentage = totalMarks > 0 ? Math.Round((totalObtained / totalMarks) * 100, 2) : 0;

                    examReports.Add(new StudentExamCategoryReport
                    {
                        ExamName = exam.Name,
                        ExamId = exam.Id,
                        SubjectMarks = subjectMarks,
                        TotalObtained = totalObtained,
                        TotalMarks = totalMarks,
                        Percentage = percentage,
                        OverallGrade = CalculateOverallGradee(percentage),
                        OverallStatus = percentage >= 33 ? "Pass" : "Fail"
                    });
                }

                viewModel.ExamReports = examReports;
            }

            return View(viewModel);
        }
        // GET: Individual Test Report Card
        public async Task<IActionResult> TestReportCard(int studentId, int examId)
        {
 
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                    .ThenInclude(sgd => sgd.Subject)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == studentId );

            var exam = await _context.Exams
                .Include(e => e.ExamCategory)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (student == null || exam == null)
            {
                return NotFound();
            }

            var examMarks = await _context.ExamMarks
                .Include(em => em.Subject)
                .Where(em => em.StudentId == studentId &&
                           em.ExamId == examId &&
                           em.IsActive)
                .ToListAsync();

            var studentSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                .Where(sgd => sgd.IsActive)
                .Select(sgd => sgd.Subject)
                .ToList() ?? new List<Subject>();

            var subjectMarks = new List<StudentSubjectMark>();
            decimal totalObtained = 0;
            decimal totalMarks = 0;

            foreach (var subject in studentSubjects)
            {
                var mark = examMarks.FirstOrDefault(em => em.SubjectId == subject.Id);
                if (mark != null)
                {
                    subjectMarks.Add(new StudentSubjectMark
                    {
                        SubjectName = subject.Name,
                        TotalMarks = mark.TotalMarks,
                        ObtainedMarks = mark.ObtainedMarks,
                        Grade = mark.Grade,
                        Status = mark.Status,
                        HasEntry = true,
                        Percentage = mark.Percentage
                    });
                    totalObtained += mark.ObtainedMarks;
                    totalMarks += mark.TotalMarks;
                }
                else
                {
                    var defaultTotalMarks = 100m;
                    subjectMarks.Add(new StudentSubjectMark
                    {
                        SubjectName = subject.Name,
                        TotalMarks = defaultTotalMarks,
                        ObtainedMarks = 0,
                        Grade = "F",
                        Status = "Absent",
                        HasEntry = false,
                        Percentage = 0
                    });
                    totalMarks += defaultTotalMarks;
                }
            }

            var overallPercentage = totalMarks > 0 ? Math.Round((totalObtained / totalMarks) * 100, 2) : 0;

            var viewModel = new TestReportCardViewModel
            {
                Student = student,
                Exam = exam,
                SubjectMarks = subjectMarks,
                TotalObtained = totalObtained,
                TotalMarks = totalMarks,
                Percentage = overallPercentage,
                OverallGrade = CalculateOverallGrade(overallPercentage),
                OverallStatus = overallPercentage >= 33 ? "Pass" : "Fail",
                Campus = student.Campus
            };

            return View(viewModel);
        }

        // GET: Class-wise Reports
        public async Task<IActionResult> ClassWiseReports(int? classId = null, int? sectionId = null, int? examCategoryId = null, int? examId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new ClassWiseReportsViewModel
            {
                Classes = campusId.HasValue
                    ? await _context.Classes
                        .Where(c => c.CampusId == campusId && c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToListAsync()
                    : await _context.Classes
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToListAsync(),
                ExamCategories = campusId.HasValue
                    ? await _context.ExamCategories
                        .Where(ec => ec.CampusId == campusId && ec.IsActive)
                        .OrderBy(ec => ec.Name)
                        .ToListAsync()
                    : await _context.ExamCategories
                        .Where(ec => ec.IsActive)
                        .OrderBy(ec => ec.Name)
                        .ToListAsync(),
                SelectedClassId = classId,
                SelectedSectionId = sectionId,
                SelectedExamCategoryId = examCategoryId,
                SelectedExamId = examId,
                Campus = campusId.HasValue ? await _context.Campuses.FindAsync(campusId) : null
            };

            if (classId.HasValue)
            {
                viewModel.Sections = await _context.ClassSections
                    .Where(cs => cs.ClassId == classId.Value && cs.IsActive)
                    .OrderBy(cs => cs.Name)
                    .ToListAsync();
            }

            if (examCategoryId.HasValue)
            {
                viewModel.Exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == examCategoryId.Value && e.IsActive)
                    .OrderBy(e => e.Name)
                    .ToListAsync();
            }

            if (classId.HasValue && sectionId.HasValue && examId.HasValue)
            {
                // Build student query with conditional campus filtering
                var studentsQuery = _context.Students
                    .Include(s => s.SubjectsGrouping)
                        .ThenInclude(sg => sg.SubjectsGroupingDetails)
                        .ThenInclude(sgd => sgd.Subject)
                    .Where(s => s.Class == classId.Value &&
                               s.Section == sectionId.Value &&
                               !s.HasLeft);

                // Apply campus filter only if campusId is not null
                if (campusId.HasValue)
                {
                    studentsQuery = studentsQuery.Where(s => s.CampusId == campusId);
                }

                var students = await studentsQuery
                    .OrderBy(s => s.StudentName)
                    .ToListAsync();

                var exam = await _context.Exams.FindAsync(examId.Value);
                var classObj = await _context.Classes.FindAsync(classId.Value);
                var section = await _context.ClassSections.FindAsync(sectionId.Value);

                // Get ALL unique subjects from ALL students (union of all subjects)
                var allSubjects = students
                    .Where(s => s.SubjectsGrouping?.SubjectsGroupingDetails != null)
                    .SelectMany(s => s.SubjectsGrouping.SubjectsGroupingDetails
                        .Where(sgd => sgd.IsActive && sgd.Subject != null)
                        .Select(sgd => sgd.Subject))
                    .GroupBy(s => s.Id)
                    .Select(g => g.First())
                    .OrderBy(s => s.Name)
                    .ToList();

                var studentReports = new List<ClassWiseStudentReport>();

                foreach (var student in students)
                {
                    var examMarks = await _context.ExamMarks
                        .Include(em => em.Subject)
                        .Where(em => em.StudentId == student.Id &&
                                   em.ExamId == examId.Value &&
                                   em.IsActive)
                        .ToListAsync();

                    var subjectMarks = new List<ClassWiseSubjectMark>();
                    decimal totalObtained = 0;
                    decimal totalMarks = 0;

                    // Get this student's enrolled subjects
                    var studentSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                        .Where(sgd => sgd.IsActive && sgd.Subject != null)
                        .Select(sgd => sgd.Subject)
                        .ToList() ?? new List<Subject>();

                    foreach (var subject in allSubjects)
                    {
                        var mark = examMarks.FirstOrDefault(em => em.SubjectId == subject.Id);
                        bool isEnrolled = studentSubjects.Any(s => s.Id == subject.Id);

                        if (mark != null)
                        {
                            // Student has marks for this subject
                            subjectMarks.Add(new ClassWiseSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = mark.TotalMarks,
                                ObtainedMarks = mark.ObtainedMarks,
                                Grade = mark.Grade,
                                IsEnrolled = true,
                                HasEntry = true
                            });
                            totalObtained += mark.ObtainedMarks;
                            totalMarks += mark.TotalMarks;
                        }
                        else if (isEnrolled)
                        {
                            // Student is enrolled but no marks entry exists
                            var defaultTotalMarks = 100m;
                            subjectMarks.Add(new ClassWiseSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = defaultTotalMarks,
                                ObtainedMarks = 0,
                                Grade = "F",
                                IsEnrolled = true,
                                HasEntry = false
                            });
                            totalMarks += defaultTotalMarks;
                        }
                        else
                        {
                            // Student is not enrolled in this subject
                            subjectMarks.Add(new ClassWiseSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = 0,
                                ObtainedMarks = 0,
                                Grade = "NE", // Not Enrolled
                                IsEnrolled = false,
                                HasEntry = false
                            });
                            // Don't add to totalMarks for non-enrolled subjects
                        }
                    }

                    var percentage = totalMarks > 0 ? Math.Round((totalObtained / totalMarks) * 100, 2) : 0;

                    studentReports.Add(new ClassWiseStudentReport
                    {
                        StudentId = student.Id,
                        StudentName = student.StudentName,
                        FatherName = student.FatherName,
                        SubjectMarks = subjectMarks,
                        TotalObtained = totalObtained,
                        TotalMarks = totalMarks,
                        Percentage = percentage,
                        Grade = CalculateOverallGrade(percentage),
                        Status = percentage >= 33 ? "Pass" : "Fail"
                    });
                }

                // Calculate positions
                var sortedStudents = studentReports
                    .Where(sr => sr.TotalMarks > 0)
                    .OrderByDescending(sr => sr.Percentage)
                    .ThenByDescending(sr => sr.TotalObtained)
                    .ToList();

                for (int i = 0; i < sortedStudents.Count; i++)
                {
                    sortedStudents[i].Position = i + 1;
                }

                viewModel.StudentReports = studentReports.OrderBy(sr => sr.Position ?? int.MaxValue).ToList();
                viewModel.AllSubjects = allSubjects;
                viewModel.SelectedExam = exam;
                viewModel.SelectedClass = classObj;
                viewModel.SelectedSection = section;

                // Calculate statistics
                if (studentReports.Any(sr => sr.TotalMarks > 0))
                {
                    var validReports = studentReports.Where(sr => sr.TotalMarks > 0).ToList();
                    viewModel.ClassAverage = validReports.Average(sr => sr.Percentage);
                    viewModel.HighestPercentage = validReports.Max(sr => sr.Percentage);
                    viewModel.LowestPercentage = validReports.Min(sr => sr.Percentage);
                    viewModel.TotalStudents = validReports.Count;
                    viewModel.PassedStudents = validReports.Count(sr => sr.Status == "Pass");
                    viewModel.FailedStudents = validReports.Count(sr => sr.Status == "Fail");
                    viewModel.PassPercentage = viewModel.TotalStudents > 0 ?
                        Math.Round((decimal)viewModel.PassedStudents / viewModel.TotalStudents * 100, 2) : 0;
                }
            }

            return View(viewModel);
        }

        public async Task<IActionResult> PrintClassReport(int? classId = null, int? sectionId = null, int? examCategoryId = null, int? examId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var viewModel = new ClassWiseReportsViewModel
            {
                Classes = campusId.HasValue
                    ? await _context.Classes
                        .Where(c => c.CampusId == campusId && c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToListAsync()
                    : await _context.Classes
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToListAsync(),
                ExamCategories = campusId.HasValue
                    ? await _context.ExamCategories
                        .Where(ec => ec.CampusId == campusId && ec.IsActive)
                        .OrderBy(ec => ec.Name)
                        .ToListAsync()
                    : await _context.ExamCategories
                        .Where(ec => ec.IsActive)
                        .OrderBy(ec => ec.Name)
                        .ToListAsync(),
                SelectedClassId = classId,
                SelectedSectionId = sectionId,
                SelectedExamCategoryId = examCategoryId,
                SelectedExamId = examId,
                Campus = campusId.HasValue ? await _context.Campuses.FindAsync(campusId) : null
            };

            if (classId.HasValue)
            {
                viewModel.Sections = await _context.ClassSections
                    .Where(cs => cs.ClassId == classId.Value && cs.IsActive)
                    .OrderBy(cs => cs.Name)
                    .ToListAsync();
            }

            if (examCategoryId.HasValue)
            {
                viewModel.Exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == examCategoryId.Value && e.IsActive)
                    .OrderBy(e => e.Name)
                    .ToListAsync();
            }

            if (classId.HasValue && sectionId.HasValue && examId.HasValue)
            {
                // Build student query with conditional campus filtering
                var studentsQuery = _context.Students
                    .Include(s => s.SubjectsGrouping)
                        .ThenInclude(sg => sg.SubjectsGroupingDetails)
                        .ThenInclude(sgd => sgd.Subject)
                    .Where(s => s.Class == classId.Value &&
                               s.Section == sectionId.Value &&
                               !s.HasLeft);

                // Apply campus filter only if campusId is not null
                if (campusId.HasValue)
                {
                    studentsQuery = studentsQuery.Where(s => s.CampusId == campusId);
                }

                var students = await studentsQuery
                    .OrderBy(s => s.StudentName)
                    .ToListAsync();

                var exam = await _context.Exams.FindAsync(examId.Value);
                var classObj = await _context.Classes.FindAsync(classId.Value);
                var section = await _context.ClassSections.FindAsync(sectionId.Value);

                // Get ALL unique subjects from ALL students (union of all subjects)
                var allSubjects = students
                    .Where(s => s.SubjectsGrouping?.SubjectsGroupingDetails != null)
                    .SelectMany(s => s.SubjectsGrouping.SubjectsGroupingDetails
                        .Where(sgd => sgd.IsActive && sgd.Subject != null)
                        .Select(sgd => sgd.Subject))
                    .GroupBy(s => s.Id)
                    .Select(g => g.First())
                    .OrderBy(s => s.Name)
                    .ToList();

                var studentReports = new List<ClassWiseStudentReport>();

                foreach (var student in students)
                {
                    var examMarks = await _context.ExamMarks
                        .Include(em => em.Subject)
                        .Where(em => em.StudentId == student.Id &&
                                   em.ExamId == examId.Value &&
                                   em.IsActive)
                        .ToListAsync();

                    var subjectMarks = new List<ClassWiseSubjectMark>();
                    decimal totalObtained = 0;
                    decimal totalMarks = 0;

                    // Get this student's enrolled subjects
                    var studentSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails?
                        .Where(sgd => sgd.IsActive && sgd.Subject != null)
                        .Select(sgd => sgd.Subject)
                        .ToList() ?? new List<Subject>();

                    foreach (var subject in allSubjects)
                    {
                        var mark = examMarks.FirstOrDefault(em => em.SubjectId == subject.Id);
                        bool isEnrolled = studentSubjects.Any(s => s.Id == subject.Id);

                        if (mark != null)
                        {
                            // Student has marks for this subject
                            subjectMarks.Add(new ClassWiseSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = mark.TotalMarks,
                                ObtainedMarks = mark.ObtainedMarks,
                                Grade = mark.Grade,
                                IsEnrolled = true,
                                HasEntry = true
                            });
                            totalObtained += mark.ObtainedMarks;
                            totalMarks += mark.TotalMarks;
                        }
                        else if (isEnrolled)
                        {
                            // Student is enrolled but no marks entry exists
                            var defaultTotalMarks = 100m;
                            subjectMarks.Add(new ClassWiseSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = defaultTotalMarks,
                                ObtainedMarks = 0,
                                Grade = "F",
                                IsEnrolled = true,
                                HasEntry = false
                            });
                            totalMarks += defaultTotalMarks;
                        }
                        else
                        {
                            // Student is not enrolled in this subject
                            subjectMarks.Add(new ClassWiseSubjectMark
                            {
                                SubjectName = subject.Name,
                                TotalMarks = 0,
                                ObtainedMarks = 0,
                                Grade = "NE", // Not Enrolled
                                IsEnrolled = false,
                                HasEntry = false
                            });
                            // Don't add to totalMarks for non-enrolled subjects
                        }
                    }

                    var percentage = totalMarks > 0 ? Math.Round((totalObtained / totalMarks) * 100, 2) : 0;

                    studentReports.Add(new ClassWiseStudentReport
                    {
                        StudentId = student.Id,
                        StudentName = student.StudentName,
                        FatherName = student.FatherName,
                        SubjectMarks = subjectMarks,
                        TotalObtained = totalObtained,
                        TotalMarks = totalMarks,
                        Percentage = percentage,
                        Grade = CalculateOverallGrade(percentage),
                        Status = percentage >= 33 ? "Pass" : "Fail"
                    });
                }

                // Calculate positions
                var sortedStudents = studentReports
                    .Where(sr => sr.TotalMarks > 0)
                    .OrderByDescending(sr => sr.Percentage)
                    .ThenByDescending(sr => sr.TotalObtained)
                    .ToList();

                for (int i = 0; i < sortedStudents.Count; i++)
                {
                    sortedStudents[i].Position = i + 1;
                }

                viewModel.StudentReports = studentReports.OrderBy(sr => sr.Position ?? int.MaxValue).ToList();
                viewModel.AllSubjects = allSubjects;
                viewModel.SelectedExam = exam;
                viewModel.SelectedClass = classObj;
                viewModel.SelectedSection = section;

                // Calculate statistics
                if (studentReports.Any(sr => sr.TotalMarks > 0))
                {
                    var validReports = studentReports.Where(sr => sr.TotalMarks > 0).ToList();
                    viewModel.ClassAverage = validReports.Average(sr => sr.Percentage);
                    viewModel.HighestPercentage = validReports.Max(sr => sr.Percentage);
                    viewModel.LowestPercentage = validReports.Min(sr => sr.Percentage);
                    viewModel.TotalStudents = validReports.Count;
                    viewModel.PassedStudents = validReports.Count(sr => sr.Status == "Pass");
                    viewModel.FailedStudents = validReports.Count(sr => sr.Status == "Fail");
                    viewModel.PassPercentage = viewModel.TotalStudents > 0 ?
                        Math.Round((decimal)viewModel.PassedStudents / viewModel.TotalStudents * 100, 2) : 0;
                }
            }

            return View("PrintClassReport", viewModel);
        } // GET: Get sections by class
        [HttpGet]
        public async Task<IActionResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(cs => cs.ClassId == classId && cs.IsActive)
                .Select(cs => new { id = cs.Id, name = cs.Name })
                .OrderBy(cs => cs.name)
                .ToListAsync();

            return Json(sections);
        }

        // GET: Get exams by category
        [HttpGet]
        public async Task<IActionResult> GetExamsByCategory(int examCategoryId)
        {
            var exams = await _context.Exams
                .Where(e => e.ExamCategoryId == examCategoryId && e.IsActive)
                .Select(e => new { id = e.Id, name = e.Name })
                .OrderBy(e => e.name)
                .ToListAsync();

            return Json(exams);
        }

        private string CalculateOverallGrade(decimal percentage)
        {
            if (percentage >= 90) return "A+";
            if (percentage >= 80) return "A";
            if (percentage >= 70) return "B";
            if (percentage >= 60) return "C";
            if (percentage >= 50) return "D";
            if (percentage >= 33) return "E";
            return "F";
        }
      

        private string CalculateOverallGradee(decimal percentage)
        {
            return percentage switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B+",
                >= 60 => "B",
                >= 50 => "C",
                >= 40 => "D",
                _ => "F"
            };
        }
        
    }
}