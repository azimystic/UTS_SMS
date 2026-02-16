using SMS.Models;

namespace SMS.ViewModels
{
    // Main index view model
    public class ExamReportsIndexViewModel
    {
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Class> Classes { get; set; } = new List<Class>();
        public Campus Campus { get; set; }
        public string SearchTerm { get; set; }
    }

    // Student search result
    public class StudentSearchResult
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public string StudentCNIC { get; set; }
    }

    // Student exam report view model
    public class StudentExamReportViewModel
    {
        public Student Student { get; set; }
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public int? SelectedExamCategoryId { get; set; }
        public List<StudentExamCategoryReport> ExamReports { get; set; } = new List<StudentExamCategoryReport>();
        public Campus Campus { get; set; }

        // Statistics
        public decimal OverallAverage { get; set; }
        public int TotalExams { get; set; }
        public int PassedExams { get; set; }
        public int FailedExams { get; set; }
        public string BestSubject { get; set; }
        public string WeakestSubject { get; set; }
    }

    // Individual exam category report
    public class StudentExamCategoryReport
    {
        public string ExamName { get; set; }
        public int ExamId { get; set; }
        public List<StudentSubjectMark> SubjectMarks { get; set; } = new List<StudentSubjectMark>();
        public decimal TotalObtained { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string OverallGrade { get; set; }
        public string OverallStatus { get; set; }
    }

    // Student subject mark
    public class StudentSubjectMark
    {
        public string SubjectName { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public string Grade { get; set; }
        public string Status { get; set; }
        public bool HasEntry { get; set; }
        public decimal Percentage { get; set; }
    }

    // Test report card view model
    public class TestReportCardViewModel
    {
        public Student Student { get; set; }
        public Exam Exam { get; set; }
        public List<StudentSubjectMark> SubjectMarks { get; set; } = new List<StudentSubjectMark>();
        public decimal TotalObtained { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string OverallGrade { get; set; }
        public string OverallStatus { get; set; }
        public Campus Campus { get; set; }
        public DateTime ReportGeneratedDate { get; set; } = DateTime.Now;
    }

    // Class-wise reports view model
    public class ClassWiseReportsViewModel
    {
        public List<Class> Classes { get; set; } = new List<Class>();
        public List<ClassSection> Sections { get; set; } = new List<ClassSection>();
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Exam> Exams { get; set; } = new List<Exam>();

        public int? SelectedClassId { get; set; }
        public int? SelectedSectionId { get; set; }
        public int? SelectedExamCategoryId { get; set; }
        public int? SelectedExamId { get; set; }

        public Class SelectedClass { get; set; }
        public ClassSection SelectedSection { get; set; }
        public Exam SelectedExam { get; set; }

        public List<ClassWiseStudentReport> StudentReports { get; set; } = new List<ClassWiseStudentReport>();
        public List<Subject> AllSubjects { get; set; } = new List<Subject>();

        public Campus Campus { get; set; }

        // Statistics
        public decimal ClassAverage { get; set; }
        public decimal HighestPercentage { get; set; }
        public decimal LowestPercentage { get; set; }
        public int TotalStudents { get; set; }
        public int PassedStudents { get; set; }
        public int FailedStudents { get; set; }
        public decimal PassPercentage { get; set; }

        // Grade distribution for charts
        public Dictionary<string, int> GradeDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> StatusDistribution { get; set; } = new Dictionary<string, int>();
    }

    // Class-wise student report
    public class ClassWiseStudentReport
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public List<ClassWiseSubjectMark> SubjectMarks { get; set; } = new List<ClassWiseSubjectMark>();
        public decimal TotalObtained { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; }
        public string Status { get; set; }
        public int? Position { get; set; }
    }

    // Class-wise subject mark
    public class ClassWiseSubjectMark
    {
        public string SubjectName { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public string Grade { get; set; }
        public bool IsEnrolled { get; set; }
        public bool HasEntry { get; set; }
    }

    // Chart data models
    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string Color { get; set; }
    }

    public class ExamAnalyticsViewModel
    {
        public List<ChartDataPoint> GradeDistribution { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> StatusDistribution { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> SubjectPerformance { get; set; } = new List<ChartDataPoint>();
        public decimal ClassAverage { get; set; }
        public decimal PassPercentage { get; set; }
        public int TotalStudents { get; set; }
    }
}