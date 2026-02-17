using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using UTS_SMS.Models;

namespace UTS_SMS.Services
{
    /// <summary>
    /// Semantic Kernel plugin that provides the AI with "hands" to query school data.
    /// The AI decides when to call these methods based on user questions.
    /// </summary>
    public class SmsPlugin
    {
        private readonly ApplicationDbContext _context;
        private readonly VectorStoreService _vectorStore;
        private readonly ILogger<SmsPlugin> _logger;

        // Set by the orchestrator before each conversation based on the logged-in user
        public int? CurrentStudentId { get; set; }
        public int? CurrentCampusId { get; set; }
        public string CurrentRole { get; set; } = "Student";
        public int? CurrentEmployeeId { get; set; }

        // Track which tools were called and their sources (for UI display)
        public List<ToolCallStep> ToolCallLog { get; } = new();
        public List<SearchResult> SourcesCited { get; } = new();

        public SmsPlugin(
            ApplicationDbContext context,
            VectorStoreService vectorStore,
            ILogger<SmsPlugin> logger)
        {
            _context = context;
            _vectorStore = vectorStore;
            _logger = logger;
        }

        [KernelFunction("GetStudentGrades")]
        [Description("Get a student's exam grades. Returns subject name, exam name, obtained marks, total marks, percentage, grade, and status (Pass/Fail). If subjectName is provided, filters to that subject only. If examName is provided, filters to that exam only.")]
        public async Task<string> GetStudentGrades(
            [Description("The student's database ID. Use the current student's ID if asking about their own grades.")] int studentId,
            [Description("Optional: filter by subject name (e.g., 'Mathematics', 'Biology')")] string? subjectName = null,
            [Description("Optional: filter by exam name")] string? examName = null)
        {
            LogToolCall("GetStudentGrades", $"Looking up grades for student #{studentId}...");

            var query = _context.ExamMarks
                .Include(m => m.Subject)
                .Include(m => m.Exam)
                .Include(m => m.Student)
                .Where(m => m.StudentId == studentId && m.IsActive);

            // Campus scoping for non-admin
            if (CurrentCampusId.HasValue)
                query = query.Where(m => m.CampusId == CurrentCampusId.Value);

            if (!string.IsNullOrWhiteSpace(subjectName))
                query = query.Where(m => m.Subject.Name.Contains(subjectName));

            if (!string.IsNullOrWhiteSpace(examName))
                query = query.Where(m => m.Exam.Name.Contains(examName));

            var marks = await query
                .OrderByDescending(m => m.ExamDate)
                .Take(50)
                .Select(m => new
                {
                    Subject = m.Subject.Name,
                    Exam = m.Exam.Name,
                    m.ObtainedMarks,
                    m.TotalMarks,
                    m.PassingMarks,
                    m.Percentage,
                    m.Grade,
                    m.Status,
                    m.ExamDate,
                    m.Remarks
                })
                .ToListAsync();

            if (marks.Count == 0)
                return "No grades found for this student" + (subjectName != null ? $" in {subjectName}" : "") + ".";

            var student = await _context.Students.FindAsync(studentId);
            var header = $"Grades for {student?.StudentName ?? "Student"} (ID: {studentId}):\n\n";

            var lines = marks.Select(m =>
                $"• {m.Subject} - {m.Exam}: {m.ObtainedMarks}/{m.TotalMarks} ({m.Percentage:F1}%) | Grade: {m.Grade} | {m.Status}" +
                (m.Remarks != null ? $" | Note: {m.Remarks}" : "")
            );

            return header + string.Join("\n", lines);
        }

        [KernelFunction("GetStudentInfo")]
        [Description("Get basic information about a student including their name, class, section, and enrolled subjects.")]
        public async Task<string> GetStudentInfo(
            [Description("The student's database ID")] int studentId)
        {
            LogToolCall("GetStudentInfo", $"Looking up student #{studentId} info...");

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return $"Student with ID {studentId} not found.";

            // Get subjects via SubjectsGrouping
            var subjects = new List<string>();
            if (student.SubjectsGroupingId > 0)
            {
                subjects = await _context.SubjectsGroupingDetails
                    .Include(d => d.Subject)
                    .Where(d => d.SubjectsGroupingId == student.SubjectsGroupingId)
                    .Select(d => d.Subject.Name)
                    .ToListAsync();
            }

            return $"Student: {student.StudentName}\n" +
                   $"Roll Number: {student.RollNumber}\n" +
                   $"Class: {student.ClassObj?.Name ?? "N/A"}, Section: {student.SectionObj?.Name ?? "N/A"}\n" +
                   $"Campus: {student.Campus?.Name ?? "N/A"}\n" +
                   $"Subjects: {(subjects.Any() ? string.Join(", ", subjects) : "N/A")}";
        }

        [KernelFunction("SearchStudyMaterials")]
        [Description("Search through uploaded PDF textbooks, syllabi, and study materials for relevant content. Use this when the user asks about course content, syllabus topics, textbook content, grading rubrics, or study guidance.")]
        public async Task<string> SearchStudyMaterials(
            [Description("The search query — what topic or content to look for in the PDFs")] string query,
            [Description("Optional: filter results to a specific subject name")] string? subjectName = null)
        {
            LogToolCall("SearchStudyMaterials", $"Searching study materials for: \"{query}\"...");

            try
            {
                var results = await _vectorStore.SearchAsync(query, 5, subjectName);

                if (results.Count == 0)
                    return "No relevant study materials found for this query. The PDF library may not have been indexed yet.";

                // Track sources for citations
                SourcesCited.AddRange(results);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Found {results.Count} relevant sections from study materials:\n");

                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    sb.AppendLine($"--- Source {i + 1}: {r.FileName} (Chapter: {r.ChapterName}, Page {r.PageNumber}) ---");
                    sb.AppendLine(r.Text);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching study materials");
                return "Unable to search study materials at this time. ChromaDB may not be running.";
            }
        }

        [KernelFunction("GetSubjectsList")]
        [Description("Get the list of subjects available for a specific class. Uses the subjects grouping assigned to students in that class.")]
        public async Task<string> GetSubjectsList(
            [Description("The class ID to get subjects for")] int classId)
        {
            LogToolCall("GetSubjectsList", "Looking up available subjects...");

            // Get distinct SubjectsGroupingIds from students in this class
            var groupingIds = await _context.Students
                .Where(s => s.Class == classId && !s.HasLeft)
                .Select(s => s.SubjectsGroupingId)
                .Distinct()
                .ToListAsync();

            var subjects = await _context.SubjectsGroupingDetails
                .Include(d => d.Subject)
                .Where(d => groupingIds.Contains(d.SubjectsGroupingId) && d.IsActive)
                .Select(d => d.Subject.Name)
                .Distinct()
                .ToListAsync();

            if (!subjects.Any())
                return $"No subjects found for class ID {classId}.";

            return "Available subjects:\n" + string.Join("\n", subjects.Select(s => $"• {s}"));
        }

        [KernelFunction("GetStudentCount")]
        [Description("Get the total number of students currently enrolled in the school or campus. Returns total count, breakdown by gender, and grade distribution.")]
        public async Task<string> GetStudentCount(
            [Description("Optional: filter by campus ID. Leave empty for all campuses.")] int? campusId = null)
        {
            LogToolCall("GetStudentCount", $"Counting students" + (campusId.HasValue ? $" in campus {campusId}" : " across all campuses"));

            var query = _context.Students.Where(s => !s.HasLeft);

            if (campusId.HasValue)
                query = query.Where(s => s.CampusId == campusId);

            if (CurrentCampusId.HasValue && CurrentRole != "Admin")
                query = query.Where(s => s.CampusId == CurrentCampusId.Value);

            var students = await query
                .Include(s => s.ClassObj)
                .ToListAsync();

            var total = students.Count;
            var boys = students.Count(s => s.Gender == "Male");
            var girls = students.Count(s => s.Gender == "Female");

            var gradeDistribution = students
                .GroupBy(s => s.ClassObj?.Name ?? "Unknown")
                .OrderBy(g => g.Key)
                .Select(g => $"  {g.Key}: {g.Count()} students")
                .ToList();

            return $"Total Students: {total}\n" +
                   $"Boys: {boys}\n" +
                   $"Girls: {girls}\n\n" +
                   $"Grade Distribution:\n{string.Join("\n", gradeDistribution)}";
        }

        [KernelFunction("GetExamsList")]
        [Description("Get a list of exams, optionally filtered by academic year. Returns exam names, categories, and dates.")]
        public async Task<string> GetExamsList(
            [Description("Optional: filter by academic year (e.g., 2025)")] int? academicYear = null)
        {
            LogToolCall("GetExamsList", "Looking up exams...");

            var query = _context.Exams
                .Include(e => e.ExamCategory)
                .Where(e => e.IsActive);

            if (CurrentCampusId.HasValue)
                query = query.Where(e => e.CampusId == null || e.CampusId == CurrentCampusId.Value);

            var exams = await query
                .OrderByDescending(e => e.Id)
                .Take(20)
                .Select(e => new { e.Name, Category = e.ExamCategory.Name })
                .ToListAsync();

            if (!exams.Any())
                return "No exams found.";

            return "Exams:\n" + string.Join("\n", exams.Select(e => $"• {e.Name} (Category: {e.Category})"));
        }

        [KernelFunction("GetClassPerformance")]
        [Description("Get class-wide performance statistics for a specific subject and exam. Returns average, highest, lowest marks, pass rate, and grade distribution. Only available to Teachers and Admins.")]
        public async Task<string> GetClassPerformance(
            [Description("Class ID")] int classId,
            [Description("Subject ID")] int subjectId,
            [Description("Exam ID")] int examId)
        {
            if (CurrentRole == "Student")
                return "Sorry, class performance data is only available to Teachers and Admins.";

            LogToolCall("GetClassPerformance", "Analyzing class performance...");

            var marks = await _context.ExamMarks
                .Include(m => m.Student)
                .Include(m => m.Subject)
                .Include(m => m.Exam)
                .Include(m => m.Class)
                .Where(m => m.ClassId == classId && m.SubjectId == subjectId && m.ExamId == examId && m.IsActive)
                .ToListAsync();

            if (!marks.Any())
                return "No marks data found for the specified class, subject, and exam combination.";

            var subject = marks.First().Subject?.Name ?? "Unknown";
            var exam = marks.First().Exam?.Name ?? "Unknown";
            var className = marks.First().Class?.Name ?? "Unknown";

            var avg = marks.Average(m => m.Percentage);
            var max = marks.Max(m => m.Percentage);
            var min = marks.Min(m => m.Percentage);
            var passRate = (double)marks.Count(m => m.Status != "Fail") / marks.Count * 100;

            var gradeDistribution = marks
                .GroupBy(m => m.Grade)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()} students")
                .ToList();

            var topStudents = marks
                .OrderByDescending(m => m.Percentage)
                .Take(5)
                .Select(m => $"  {m.Student?.StudentName}: {m.Percentage:F1}% ({m.Grade})")
                .ToList();

            return $"Class Performance - {className} | {subject} | {exam}\n\n" +
                   $"  Students: {marks.Count}\n" +
                   $"  Average: {avg:F1}%\n" +
                   $"  Highest: {max:F1}%\n" +
                   $"  Lowest:  {min:F1}%\n" +
                   $"  Pass Rate: {passRate:F1}%\n\n" +
                   $"Grade Distribution:\n  {string.Join("\n  ", gradeDistribution)}\n\n" +
                   $"Top 5 Students:\n{string.Join("\n", topStudents)}";
        }

        [KernelFunction("GetStudentAttendanceSummary")]
        [Description("Get a student's attendance summary showing total present, absent, late days and attendance percentage.")]
        public async Task<string> GetStudentAttendanceSummary(
            [Description("The student's database ID")] int studentId)
        {
            LogToolCall("GetStudentAttendanceSummary", "Checking attendance records...");

            var attendance = await _context.Attendance
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            if (!attendance.Any())
                return $"No attendance records found for student #{studentId}.";

            var total = attendance.Count;
            var present = attendance.Count(a => a.Status == "P");
            var absent = attendance.Count(a => a.Status == "A");
            var leave = attendance.Count(a => a.Status == "L");
            var late = 0; // Not tracked separately
            var percentage = total > 0 ? (double)present / total * 100 : 0;

            return $"Attendance Summary:\n" +
                   $"  Total Days: {total}\n" +
                   $"  Present: {present} ({percentage:F1}%)\n" +
                   $"  Absent: {absent}\n" +
                   $"  Late: {late}\n" +
                   $"  Leave: {leave}";
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private void LogToolCall(string toolName, string description)
        {
            _logger.LogInformation("AI calling tool: {Tool} - {Desc}", toolName, description);
            ToolCallLog.Add(new ToolCallStep { ToolName = toolName, Description = description, Timestamp = DateTime.Now });
        }
    }

    public class ToolCallStep
    {
        public string ToolName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
