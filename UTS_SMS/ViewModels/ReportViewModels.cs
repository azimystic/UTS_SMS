using SMS.Models;
using System.ComponentModel.DataAnnotations;

namespace SMS.ViewModels
{
    // Base Report Filter ViewModel
    public class ReportFilterViewModel
    {
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Exam> Exams { get; set; } = new List<Exam>();
        public List<Class> Classes { get; set; } = new List<Class>();
        public List<ClassSection> Sections { get; set; } = new List<ClassSection>();
        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public List<Campus> Campuses { get; set; } = new List<Campus>();
    }

    // Award Sheet ViewModel
    public class AwardSheetViewModel : ReportFilterViewModel
    {
        [Required(ErrorMessage = "Class is required")]
        public int? ClassId { get; set; }

        [Required(ErrorMessage = "Section is required")]
        public int? SectionId { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public int? SubjectId { get; set; }

        [Required(ErrorMessage = "Exam Category is required")]
        public int? ExamCategoryId { get; set; }

        [Required(ErrorMessage = "Exam is required")]
        public int? ExamId { get; set; }

        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? SubjectName { get; set; }
        public string? ExamName { get; set; }
        public string? ExamCategoryName { get; set; }

        public List<StudentMarksData> Students { get; set; } = new List<StudentMarksData>();
    }

    // Class Exam Report (Broadsheet) ViewModel
    public class ClassExamReportViewModel : ReportFilterViewModel
    {
        [Required(ErrorMessage = "Class is required")]
        public int? ClassId { get; set; }

        [Required(ErrorMessage = "Section is required")]
        public int? SectionId { get; set; }

        [Required(ErrorMessage = "Exam Category is required")]
        public int? ExamCategoryId { get; set; }

        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? ExamCategoryName { get; set; }

        public List<StudentBroadsheetData> Students { get; set; } = new List<StudentBroadsheetData>();
        public List<string> SubjectNames { get; set; } = new List<string>();
    }

    // Student Report Card ViewModel
    public class StudentReportCardViewModel : ReportFilterViewModel
    {
        [Required(ErrorMessage = "Student is required")]
        public int? StudentId { get; set; }

        [Required(ErrorMessage = "Exam Category is required")]
        public int? ExamCategoryId { get; set; }

        [Required(ErrorMessage = "Exam is required")]
        public int? ExamId { get; set; }

        public string? StudentName { get; set; }
        public string? FatherName { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? ExamName { get; set; }
        public string? ExamCategoryName { get; set; }
        public string? RollNumber { get; set; }

        public List<SubjectMarksData> SubjectMarks { get; set; } = new List<SubjectMarksData>();
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public string? Grade { get; set; }
        public int? Position { get; set; }
    }

    // Report Card with History ViewModel
    public class ReportCardHistoryViewModel : ReportFilterViewModel
    {
        [Required(ErrorMessage = "Student is required")]
        public int? StudentId { get; set; }

        [Required(ErrorMessage = "Exam Category is required")]
        public int? ExamCategoryId { get; set; }

        [Required(ErrorMessage = "Focus Exam is required")]
        public int? FocusExamId { get; set; }

        public string? StudentName { get; set; }
        public string? FatherName { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? RollNumber { get; set; }

        // Focus exam data
        public StudentReportCardData FocusExamData { get; set; } = new StudentReportCardData();

        // Historical exam data
        public List<StudentReportCardData> HistoricalExams { get; set; } = new List<StudentReportCardData>();
    }

    // Bulk Report Card ViewModel
    public class BulkReportCardViewModel : ReportFilterViewModel
    {
        [Required(ErrorMessage = "Class is required")]
        public int? ClassId { get; set; }

        [Required(ErrorMessage = "Section is required")]
        public int? SectionId { get; set; }

        [Required(ErrorMessage = "Exam Category is required")]
        public int? ExamCategoryId { get; set; }

        [Required(ErrorMessage = "Exam is required")]
        public int? ExamId { get; set; }

        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? ExamName { get; set; }
        public string? ExamCategoryName { get; set; }

        public int StudentCount { get; set; }
    }

    // Supporting Data Classes
    public class StudentMarksData
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public decimal? ObtainedMarks { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
        public string? Status { get; set; }
        public bool IsEnrolled { get; set; }
    }

    public class StudentBroadsheetData
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public Dictionary<string, SubjectMarksData> SubjectMarks { get; set; } = new Dictionary<string, SubjectMarksData>();
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public string? Grade { get; set; }
        public int Position { get; set; }
    }

    public class SubjectMarksData
    {
        public string SubjectName { get; set; } = string.Empty;
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public string? Grade { get; set; }
        public string? Status { get; set; }
        public bool IsEnrolled { get; set; }
    }

    public class StudentReportCardData
    {
        public string ExamName { get; set; } = string.Empty;
        public List<SubjectMarksData> SubjectMarks { get; set; } = new List<SubjectMarksData>();
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public string? Grade { get; set; }
        public int? Position { get; set; }
        public DateTime? ExamDate { get; set; }
    }
}
