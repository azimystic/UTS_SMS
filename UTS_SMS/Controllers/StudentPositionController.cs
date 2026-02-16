using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
 using SMS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Controllers
{
    public class StudentPositionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public StudentPositionController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: StudentPosition
        public async Task<IActionResult> Index()
        {
            var filters = new PositionFiltersDto();

            // Check if position calculation is for all campuses
            var positionCalcAllCampuses = _configuration.GetValue<bool>("PositionCalculatorAllCampuses", false);

            // Load exam categories - if setting is true, only show categories with null CampusId
            if (positionCalcAllCampuses)
            {
                filters.ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && ec.CampusId == null)
                    .OrderBy(ec => ec.Name)
                    .ToListAsync();
            }
            else
            {
                filters.ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive)
                    .OrderBy(ec => ec.Name)
                    .ToListAsync();
            }

            return View(filters);
        }

        // GET: Get Exams by Category
        [HttpGet]
        public async Task<IActionResult> GetExamsByCategory(int examCategoryId)
        {
            // Check if position calculation is for all campuses
            var positionCalcAllCampuses = _configuration.GetValue<bool>("PositionCalculatorAllCampuses", false);

            IQueryable<Exam> examQuery = _context.Exams
                .Where(e => e.ExamCategoryId == examCategoryId && e.IsActive);

            // If setting is true, only show exams with null CampusId
            if (positionCalcAllCampuses)
            {
                examQuery = examQuery.Where(e => e.CampusId == null);
            }

            var exams = await examQuery
                .OrderBy(e => e.Name)
                .Select(e => new { Id = e.Id, Name = e.Name })
                .ToListAsync();

            return Json(exams);
        }

        // GET: Get Classes by Exam
        [HttpGet]
        public async Task<IActionResult> GetClassesByExam(int examId)
        {
            // Get the exam with its related data
            var exam = await _context.Exams
                .Include(e => e.ExamCategory)
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive);

            if (exam == null)
            {
                return Json(new List<object>());
            }

            // Get all students who have exam marks for this exam
            var classIds = await _context.ExamMarks
                .Where(em => em.ExamId == examId && em.IsActive)
                .Select(em => em.Student.Class)
                .Distinct()
                .ToListAsync();

            // Get unique classes with grades 9-12
            var classes = await _context.Classes
                .Where(c => c.IsActive && 
                           (c.GradeLevel == "9" || c.GradeLevel == "10" || 
                            c.GradeLevel == "11" || c.GradeLevel == "12"))
                .OrderBy(c => c.GradeLevel)
                .ThenBy(c => c.Name)
                .Select(c => new { 
                    Id = c.Id, 
                    Name = c.Name, 
                    GradeLevel = c.GradeLevel,
                    CurrentAcademicYear = c.CurrentAcademicYear 
                })
                .ToListAsync();

            return Json(classes);
        }

        // GET: Get Available Academic Years for Class
        [HttpGet]
        public async Task<IActionResult> GetAcademicYearsForClass(int classId)
        {
            var classObj = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && c.IsActive);

            if (classObj == null)
            {
                return Json(new { currentYear = DateTime.Now.Year, availableYears = new List<int>() });
            }

            // Get academic years from exam marks for students in this class
            var years = await _context.ExamMarks
                .Where(em => em.Student.Class == classId && em.IsActive)
                .Select(em => em.AcademicYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Parse current academic year (e.g., "2024-2025" -> 2024)
            int currentYear = DateTime.Now.Year;
            if (!string.IsNullOrEmpty(classObj.CurrentAcademicYear))
            {
                var yearParts = classObj.CurrentAcademicYear.Split('-');
                if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                {
                    currentYear = parsedYear;
                }
            }

            // Ensure current year is in the list
            if (!years.Contains(currentYear))
            {
                years.Add(currentYear);
                years = years.OrderByDescending(y => y).ToList();
            }

            return Json(new { currentYear = currentYear, availableYears = years });
        }

        // POST: Compute Positions
        [HttpPost]
        public async Task<IActionResult> ComputePositions([FromBody] PositionComputationRequestDto request)
        {
            try
            {
                // Check if positions already exist
                var existingPositions = await _context.StudentHistories
                    .Where(sh => sh.ExamId == request.ExamId &&
                                sh.AcademicYear == request.AcademicYear &&
                                sh.ClassId == request.ClassId &&
                                 sh.IsActive)
                    .ToListAsync();

                // Get student performance data
                var studentPerformances = await GetStudentPerformances(request);

                if (!studentPerformances.Any())
                {
                    return Json(new { success = false, message = "No exam marks found for the selected criteria." });
                }

                // Compute positions
                var positions = ComputeTopPositions(studentPerformances);

                // Check for changes if existing positions found
                bool hasChanges = false;
                if (existingPositions.Any())
                {
                    hasChanges = CheckForChanges(existingPositions, positions);
                }

                return Json(new
                {
                    success = true,
                    positions = positions,
                    hasExistingPositions = existingPositions.Any(),
                    hasChanges = hasChanges,
                    totalStudents = studentPerformances.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error computing positions: " + ex.Message });
            }
        }

        // POST: Save Positions
        [HttpPost]
        public async Task<IActionResult> SavePositions([FromBody] SavePositionsRequestDto request)
        {
            try
            {
                // If overwriting, delete existing records for this class
                if (request.OverwriteExisting)
                {
                    var existingPositions = await _context.StudentHistories
                        .Where(sh => sh.ExamId == request.ExamId &&
                                    sh.AcademicYear == request.AcademicYear &&
                                    sh.ClassId == request.ClassId &&
                                     sh.IsActive)
                        .ToListAsync();

                    _context.StudentHistories.RemoveRange(existingPositions);
                }

                // Create new student history records
                var newHistories = new List<StudentHistory>();
                foreach (var position in request.Positions)
                {
                    var history = new StudentHistory
                    {
                        StudentId = position.StudentId,
                        ExamId = request.ExamId,
                        AcademicYear = request.AcademicYear,
                        ClassId = await GetStudentClassId(position.StudentId),
                        SectionId = await GetStudentSectionId(position.StudentId),
                        Award = position.Award,
                        Position = position.Position,
                        FinalPercentage = position.FinalPercentage,
                        ComputedBy = User.Identity?.Name ?? "System",
                         IsActive = true
                    };
                    newHistories.Add(history);
                }

                _context.StudentHistories.AddRange(newHistories);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Successfully saved positions for {newHistories.Count} students."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving positions: " + ex.Message });
            }
        }

        // GET: Get Existing Positions
        [HttpGet]
        public async Task<IActionResult> GetExistingPositions(int examId, int academicYear, int classId)
        {
            var existingPositions = await _context.StudentHistories
                .Include(sh => sh.Student)
                .Include(sh => sh.Class)
                .Include(sh => sh.Section)
                .Where(sh => sh.ExamId == examId &&
                            sh.AcademicYear == academicYear &&
                            sh.ClassId == classId &&
                             sh.IsActive)
                .OrderBy(sh => sh.Position)
                .Select(sh => new StudentPositionDto
                {
                    StudentId = sh.StudentId,
                    StudentName = sh.Student.StudentName,
                    FatherName = sh.Student.FatherName,
                    ClassName = sh.Class.Name,
                    SectionName = sh.Section.Name,
                    FinalPercentage = sh.FinalPercentage,
                    Position = sh.Position,
                    Award = sh.Award,
                    AwardIcon = GetAwardIcon(sh.Position),
                    AwardColor = GetAwardColor(sh.Position),
                    HasExistingRecord = true,
                    ExistingHistoryId = sh.Id
                })
                .ToListAsync();

            return Json(new { success = true, positions = existingPositions });
        }

        #region Private Methods

        private async Task<List<StudentPerformanceSummaryDto>> GetStudentPerformances(PositionComputationRequestDto request)
        {
            // EXCEPTION: Position calculation is for ALL CAMPUSES (as per requirement)
            // This is the ONLY place where data spans across campuses intentionally
            // Check if position calculation is for all campuses
            var positionCalcAllCampuses = _configuration.GetValue<bool>("PositionCalculatorAllCampuses", false);
            
            // Default total marks for absent subjects
            const decimal DEFAULT_TOTAL_MARKS = 100m;

            // Get the exam to find tests across all campuses
            var exam = await _context.Exams
                .Include(e => e.ExamCategory)
                .FirstOrDefaultAsync(e => e.Id == request.ExamId);

            if (exam == null) return new List<StudentPerformanceSummaryDto>();

            // If PositionCalculatorAllCampuses is true, find all tests with the same name and category across all campuses
            List<int> examIds;
            if (positionCalcAllCampuses && exam.CampusId == null)
            {
                // Get all exams with same name and category across all campuses
                examIds = await _context.Exams
                    .Where(e => e.Name == exam.Name && 
                               e.ExamCategoryId == exam.ExamCategoryId && 
                               e.IsActive)
                    .Select(e => e.Id)
                    .ToListAsync();
            }
            else
            {
                examIds = new List<int> { request.ExamId };
            }

            // Get all students in the specified class only
            var students = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                        .ThenInclude(sgd => sgd.Subject)
                .Where(s => s.Class == request.ClassId && !s.HasLeft)
                .ToListAsync();

            var studentPerformances = new List<StudentPerformanceSummaryDto>();

            foreach (var student in students)
            {
                // Get student's subject IDs from grouping
                var studentSubjectIds = student.SubjectsGrouping?.SubjectsGroupingDetails
                    ?.Where(sgd => sgd.IsActive)
                    .Select(sgd => sgd.SubjectId)
                    .ToList() ?? new List<int>();

                if (!studentSubjectIds.Any())
                {
                    // Skip students without subject grouping (not enrolled in any subjects)
                    continue;
                }

                // Get exam marks for this student from all matching exams
                var examMarks = await _context.ExamMarks
                    .Include(em => em.Subject)
                    .Where(em => examIds.Contains(em.ExamId) &&
                                em.StudentId == student.Id &&
                                em.AcademicYear == request.AcademicYear &&
                                em.IsActive)
                    .ToListAsync();

                // Filter marks to only include subjects in student's grouping
                var validMarks = examMarks
                    .Where(em => studentSubjectIds.Contains(em.SubjectId))
                    .ToList();

                // Create subject performances - only count subjects student is enrolled in
                var subjectPerformances = new List<SubjectPerformanceDto>();
                decimal totalObtained = 0;
                decimal totalMax = 0;
                int subjectsWithMarks = 0;
                
                // Get subjects already loaded with student grouping to avoid N+1 query
                var studentSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails
                    ?.Where(sgd => sgd.IsActive)
                    .Select(sgd => new { sgd.SubjectId, sgd.Subject.Name })
                    .ToDictionary(s => s.SubjectId, s => s.Name) ?? new Dictionary<int, string>();

                foreach (var subjectId in studentSubjectIds)
                {
                    var mark = validMarks.FirstOrDefault(em => em.SubjectId == subjectId);
                    if (mark != null)
                    {
                        // Only count if marks exist (0 or any number)
                        // Don't count if marks are null or status indicates no data
                        bool shouldCount = mark.ObtainedMarks >= 0;
                        
                        subjectPerformances.Add(new SubjectPerformanceDto
                        {
                            SubjectName = mark.Subject.Name,
                            ObtainedMarks = mark.ObtainedMarks,
                            TotalMarks = mark.TotalMarks,
                            Percentage = mark.Percentage,
                            Grade = mark.Grade,
                            Status = mark.Status,
                            IsEnrolled = true,
                            IsCounted = shouldCount
                        });
                        
                        if (shouldCount)
                        {
                            totalObtained += mark.ObtainedMarks;
                            totalMax += mark.TotalMarks;
                            subjectsWithMarks++;
                        }
                    }
                    else
                    {
                        // Subject in grouping but no marks - don't count at all
                        var subjectName = studentSubjects.TryGetValue(subjectId, out var name) ? name : "Unknown";
                        
                        subjectPerformances.Add(new SubjectPerformanceDto
                        {
                            SubjectName = subjectName,
                            ObtainedMarks = 0,
                            TotalMarks = 0,
                            Percentage = 0,
                            Grade = "-",
                            Status = "No Marks",
                            IsEnrolled = true,
                            IsCounted = false
                        });
                    }
                }

                // Only include students who have at least one subject with marks
                if (subjectsWithMarks == 0)
                {
                    continue;
                }

                var finalPercentage = totalMax > 0 ? Math.Round((totalObtained / totalMax) * 100, 2) : 0;

                studentPerformances.Add(new StudentPerformanceSummaryDto
                {
                    StudentId = student.Id,
                    StudentName = student.StudentName,
                    FatherName = student.FatherName,
                    ClassName = student.ClassObj.Name,
                    SectionName = student.SectionObj.Name,
                    TotalSubjects = subjectsWithMarks,
                    TotalObtainedMarks = totalObtained,
                    TotalMaxMarks = totalMax,
                    FinalPercentage = finalPercentage,
                    SubjectPerformances = subjectPerformances
                });
            }

            return studentPerformances.OrderByDescending(sp => sp.FinalPercentage).ToList();
        }

        private List<StudentPositionDto> ComputeTopPositions(List<StudentPerformanceSummaryDto> performances)
        {
            var positions = new List<StudentPositionDto>();

            // Take top 9 performers
            var topPerformers = performances.Take(9).ToList();

            for (int i = 0; i < topPerformers.Count; i++)
            {
                var student = topPerformers[i];
                var position = i + 1;
                var award = AwardTypes.GetAward(position);

                positions.Add(new StudentPositionDto
                {
                    StudentId = student.StudentId,
                    StudentName = student.StudentName,
                    FatherName = student.FatherName,
                    ClassName = student.ClassName,
                    SectionName = student.SectionName,
                    FinalPercentage = student.FinalPercentage,
                    Position = position,
                    Award = award.Award,
                    AwardIcon = award.Icon,
                    AwardColor = award.Color,
                    HasExistingRecord = false
                });
            }

            return positions;
        }

        private bool CheckForChanges(List<StudentHistory> existingPositions, List<StudentPositionDto> newPositions)
        {
            if (existingPositions.Count != newPositions.Count)
                return true;

            for (int i = 0; i < existingPositions.Count; i++)
            {
                var existing = existingPositions.OrderBy(ep => ep.Position).ToList()[i];
                var newPos = newPositions[i];

                if (existing.StudentId != newPos.StudentId ||
                    existing.Position != newPos.Position ||
                    existing.FinalPercentage != newPos.FinalPercentage ||
                    existing.Award != newPos.Award)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<int> GetStudentClassId(int studentId)
        {
            var student = await _context.Students
                .Where(s => s.Id == studentId)
                .Select(s => s.Class)
                .FirstOrDefaultAsync();
            return student;
        }

        private async Task<int> GetStudentSectionId(int studentId)
        {
            var student = await _context.Students
                .Where(s => s.Id == studentId)
                .Select(s => s.Section)
                .FirstOrDefaultAsync();
            return student;
        }

        private string GetAwardIcon(int position)
        {
            return AwardTypes.GetAward(position).Icon;
        }

        private string GetAwardColor(int position)
        {
            return AwardTypes.GetAward(position).Color;
        }

        // GET: Get Student Performance Details
        [HttpGet]
        public async Task<IActionResult> GetStudentPerformanceDetails(int studentId, int examId, int academicYear)
        {
            // Check if position calculation is for all campuses
            var positionCalcAllCampuses = _configuration.GetValue<bool>("PositionCalculatorAllCampuses", false);

            // Get the exam to find tests across all campuses
            var exam = await _context.Exams
                .Include(e => e.ExamCategory)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return Json(new List<SubjectPerformanceDto>());

            // Get all matching exam IDs
            List<int> examIds;
            if (positionCalcAllCampuses && exam.CampusId == null)
            {
                examIds = await _context.Exams
                    .Where(e => e.Name == exam.Name && 
                               e.ExamCategoryId == exam.ExamCategoryId && 
                               e.IsActive)
                    .Select(e => e.Id)
                    .ToListAsync();
            }
            else
            {
                examIds = new List<int> { examId };
            }

            // Get student with subject grouping
            var student = await _context.Students
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                        .ThenInclude(sgd => sgd.Subject)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return Json(new List<SubjectPerformanceDto>());

            var studentSubjectIds = student.SubjectsGrouping?.SubjectsGroupingDetails
                ?.Where(sgd => sgd.IsActive)
                .Select(sgd => sgd.SubjectId)
                .ToList() ?? new List<int>();

            var performance = await _context.ExamMarks
                .Include(em => em.Subject)
                .Where(em => examIds.Contains(em.ExamId) &&
                            em.StudentId == studentId &&
                            em.AcademicYear == academicYear &&
                            em.IsActive &&
                            studentSubjectIds.Contains(em.SubjectId))
                .Select(em => new SubjectPerformanceDto
                {
                    SubjectName = em.Subject.Name,
                    ObtainedMarks = em.ObtainedMarks,
                    TotalMarks = em.TotalMarks,
                    Percentage = em.Percentage,
                    Grade = em.Grade,
                    Status = em.Status,
                    IsEnrolled = true,
                    IsCounted = em.ObtainedMarks >= 0
                })
                .OrderBy(sp => sp.SubjectName)
                .ToListAsync();

            // Add subjects from grouping that don't have marks
            var existingSubjectNames = performance.Select(p => p.SubjectName).ToHashSet();
            var missingSubjects = student.SubjectsGrouping?.SubjectsGroupingDetails
                ?.Where(sgd => sgd.IsActive && !existingSubjectNames.Contains(sgd.Subject.Name))
                .Select(sgd => new SubjectPerformanceDto
                {
                    SubjectName = sgd.Subject.Name,
                    ObtainedMarks = 0,
                    TotalMarks = 0,
                    Percentage = 0,
                    Grade = "-",
                    Status = "No Marks",
                    IsEnrolled = true,
                    IsCounted = false
                })
                .ToList() ?? new List<SubjectPerformanceDto>();

            performance.AddRange(missingSubjects);

            return Json(performance.OrderBy(p => p.SubjectName).ToList());
        }

        // GET: Check if positions exist
        [HttpGet]
        public async Task<IActionResult> CheckExistingPositions(int examId, int academicYear, int classId)
        {
            var count = await _context.StudentHistories
                .CountAsync(sh => sh.ExamId == examId &&
                                 sh.AcademicYear == academicYear &&
                                 sh.ClassId == classId &&
                                  sh.IsActive);

            return Json(new { exists = count > 0, count = count });
        }

        // GET: Get Position Statistics
        [HttpGet]
        public async Task<IActionResult> GetPositionStatistics(int examId, int academicYear )
        {
            var stats = await _context.StudentHistories
                .Include(sh => sh.Class)
                .Where(sh => sh.ExamId == examId &&
                            sh.AcademicYear == academicYear &&
                             sh.IsActive)
                .GroupBy(sh => sh.Class.GradeLevel)
                .Select(g => new {
                    GradeLevel = g.Key,
                    StudentCount = g.Count(),
                    AveragePercentage = g.Average(sh => sh.FinalPercentage),
                    HighestPercentage = g.Max(sh => sh.FinalPercentage),
                    Awards = g.GroupBy(sh => sh.Award)
                            .Select(ag => new { Award = ag.Key, Count = ag.Count() })
                            .ToList()
                })
                .ToListAsync();

            return Json(stats);
        }

        // POST: Recalculate Positions
        [HttpPost]
        public async Task<IActionResult> RecalculatePositions([FromBody] PositionComputationRequestDto request)
        {
            try
            {
                // Get fresh performance data
                var studentPerformances = await GetStudentPerformances(request);

                if (!studentPerformances.Any())
                {
                    return Json(new { success = false, message = "No exam marks found for recalculation." });
                }

                // Compute new positions
                var newPositions = ComputeTopPositions(studentPerformances);

                // Get existing positions
                var existingPositions = await _context.StudentHistories
                    .Include(sh => sh.Student)
                    .Include(sh => sh.Class)
                    .Include(sh => sh.Section)
                    .Where(sh => sh.ExamId == request.ExamId &&
                                sh.AcademicYear == request.AcademicYear &&
                                sh.ClassId == request.ClassId &&
                                 sh.IsActive)
                    .OrderBy(sh => sh.Position)
                    .ToListAsync();

                // Mark existing positions for comparison
                foreach (var newPos in newPositions)
                {
                    var existing = existingPositions.FirstOrDefault(ep => ep.StudentId == newPos.StudentId);
                    if (existing != null)
                    {
                        newPos.HasExistingRecord = true;
                        newPos.ExistingHistoryId = existing.Id;
                    }
                }

                var hasChanges = CheckForChanges(existingPositions, newPositions);

                return Json(new
                {
                    success = true,
                    positions = newPositions,
                    hasChanges = hasChanges,
                    existingCount = existingPositions.Count,
                    newCount = newPositions.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error recalculating positions: " + ex.Message });
            }
        }

        // POST: Delete Existing Positions
        [HttpPost]
        public async Task<IActionResult> DeleteExistingPositions(int examId, int academicYear, int classId)
        {
            try
            {
                var existingPositions = await _context.StudentHistories
                    .Where(sh => sh.ExamId == examId &&
                                sh.AcademicYear == academicYear &&
                                sh.ClassId == classId &&
                                 sh.IsActive)
                    .ToListAsync();

                _context.StudentHistories.RemoveRange(existingPositions);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted {existingPositions.Count} existing position records."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting positions: " + ex.Message });
            }
        }

        #endregion
    }
}