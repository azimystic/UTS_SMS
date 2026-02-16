using UTS_SMS.Models;

namespace SMS.ViewModels
{
    public class DashboardViewModel
    {
        // Basic counts
        public int TotalStudents { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalUsers { get; set; }
        
        // Recent data
        public List<Student> RecentStudents { get; set; } = new();
        public List<Employee> RecentEmployees { get; set; } = new();
        
        // Enhanced analytics
        public AttendanceSummary AttendanceSummary { get; set; } = new();
        public NamazAttendanceSummary NamazAttendanceSummary { get; set; } = new();
        public FinancialSummary FinancialSummary { get; set; } = new();
        public InquirySummary InquirySummary { get; set; } = new();
        public ComplaintSummary ComplaintSummary { get; set; } = new();
        public List<BillingTransaction> RecentFees { get; set; } = new();
        public List<PayrollTransaction> RecentPayroll { get; set; } = new();
        public List<AssignedDuty> ActiveDuties { get; set; } = new();
        public TeacherOfMonthViewModel? TeacherOfMonth { get; set; }
        public ExamSummary ExamSummary { get; set; } = new();
        public TestReturnsSummary TestReturnsSummary { get; set; } = new();
        
        // Campus info for Owner dashboard
        public List<Campus> Campuses { get; set; } = new();
        public int? SelectedCampusId { get; set; }
        public bool IsOwnerDashboard { get; set; }
        
        // Additional features
        public List<BirthdayInfo> UpcomingBirthdays { get; set; } = new();
        public List<ToDoItem> ToDoList { get; set; } = new();
        public List<DiaryInfo> TodayDiaries { get; set; } = new();
        public decimal TodayFeeReceived { get; set; }
        public int TodayFeeTransactions { get; set; }
    }
    
    public class DiaryInfo
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Homework { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PendingCount { get; set; }
    }
    
    public class ToDoItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
    }

    public class AttendanceSummary
    {
        public int TotalStudentsPresent { get; set; }
        public int TotalStudentsAbsent { get; set; }
        public int TotalTeachersPresent { get; set; }
        public int TotalTeachersAbsent { get; set; }
        public double StudentAttendancePercentage { get; set; }
        public double TeacherAttendancePercentage { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public List<ChartDataPoint> StudentAttendanceChart { get; set; } = new();
        public List<ChartDataPoint> TeacherAttendanceChart { get; set; } = new();
    }

    public class NamazAttendanceSummary
    {
        public int StudentsWithJamat { get; set; }
        public int StudentsQaza { get; set; }
        public int StudentsWithoutJamat { get; set; }
        public int EmployeesWithJamat { get; set; }
        public int EmployeesQaza { get; set; }
        public int EmployeesWithoutJamat { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        
        // Computed properties for easier access in views
        public int TotalStudents => StudentsWithJamat + StudentsQaza + StudentsWithoutJamat;
        public int TotalEmployees => EmployeesWithJamat + EmployeesQaza + EmployeesWithoutJamat;
        public double StudentNamazPercentage => TotalStudents > 0 
            ? Math.Round((double)StudentsWithJamat / TotalStudents * 100, 1) 
            : 0;
        public double EmployeeNamazPercentage => TotalEmployees > 0 
            ? Math.Round((double)EmployeesWithJamat / TotalEmployees * 100, 1) 
            : 0;
    }

    public class FinancialSummary
    {
        public decimal MonthlyRevenueActual { get; set; }
        public decimal MonthlyRevenueExpected { get; set; }
        public double RevenuePercentage { get; set; }
        public decimal TotalSalariesPaid { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal AssetValue { get; set; }
        public decimal TotalExpenditures { get; set; }
        public decimal ProfitLoss { get; set; }
        public double ProfitLossPercentage { get; set; }
        public decimal TotalFeesToBeCollected { get; set; }
        public int Month { get; set; } = DateTime.Now.Month;
        public int Year { get; set; } = DateTime.Now.Year;
        public string CampusName { get; set; } = "All Campuses";
    }
    
    public class BirthdayInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Student" or "Employee"
        public DateTime DateOfBirth { get; set; }
        public int DaysUntil { get; set; }
    }

    public class InquirySummary
    {
        public int TotalInquiries { get; set; }
        public int AdmissionsTaken { get; set; }
        public int PendingInquiries { get; set; }
        public int RejectedInquiries { get; set; }
        public double AdmissionRate { get; set; }
        public List<AdmissionInquiryDetail> ThisMonthInquiries { get; set; } = new();
        public List<AdmissionInquiryDetail> LastMonthInquiries { get; set; } = new();
        public int ThisMonthTotal { get; set; }
        public int ThisMonthEnrolled { get; set; }
        public int ThisMonthRejected { get; set; }
        public int LastMonthTotal { get; set; }
        public int LastMonthEnrolled { get; set; }
        public int LastMonthRejected { get; set; }
    }

    public class AdmissionInquiryDetail
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string InquiryStatus { get; set; } = string.Empty;
        public DateTime InquiryDate { get; set; }
    }

    public class ComplaintSummary
    {
        public int TotalPendingComplaints { get; set; }
        public int OpenComplaints { get; set; }
        public int InvestigationComplaints { get; set; }
        public int EscalatedComplaints { get; set; }
        public List<ComplaintDetail> AdminTeacherComplaints { get; set; } = new();
        public List<ComplaintDetail> StudentParentComplaints { get; set; } = new();
    }
    
    public class ComplaintDetail
    {
        public int Id { get; set; }
        public string ComplaintTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReporterType { get; set; } = string.Empty;
        public DateTime ComplaintDate { get; set; }
        public bool IsToday { get; set; }
        public bool IsYesterday { get; set; }
    }

    public class TeacherOfMonthViewModel
    {
        public Employee Teacher { get; set; } = new();
        public decimal Score { get; set; }
        public string Achievements { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class ExamSummary
    {
        public int TotalExamsScheduled { get; set; }
        public int ExamsCompleted { get; set; }
        public int ExamsInProgress { get; set; }
        public int ExamsPending { get; set; }
        public int TotalStudentExamEntries { get; set; }
        public int PassedStudents { get; set; }
        public int FailedStudents { get; set; }
        public double OverallPassPercentage { get; set; }
        public double AverageMarksPercentage { get; set; }
        public List<ExamCategoryStats> ExamCategoryStats { get; set; } = new();
        public List<RecentExamResult> RecentResults { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class ExamCategoryStats
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ExamsCount { get; set; }
        public double PassPercentage { get; set; }
        public int StudentEntries { get; set; }
    }

    public class RecentExamResult
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public int StudentsParticipated { get; set; }
        public double PassPercentage { get; set; }
    }

    public class TestReturnsSummary
    {
        public int TotalTestsScheduled { get; set; }
        public int TestsReturnedOnTime { get; set; }
        public int TestsReturnedLate { get; set; }
        public int TestsPendingReturn { get; set; }
        public double OnTimeReturnPercentage { get; set; }
        public int TestsWithGoodChecking { get; set; }
        public int TestsWithBetterChecking { get; set; }
        public int TestsWithBadChecking { get; set; }
        public List<TeacherTestPerformance> TeacherPerformance { get; set; } = new();
        public List<RecentTestReturn> RecentReturns { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class TeacherTestPerformance
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int TestsAssigned { get; set; }
        public int TestsReturned { get; set; }
        public int OnTimeReturns { get; set; }
        public double OnTimePercentage { get; set; }
        public string AverageCheckingQuality { get; set; } = string.Empty;
    }

    public class RecentTestReturn
    {
        public int TestReturnId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturnedOnTime { get; set; }
        public string CheckingQuality { get; set; } = string.Empty;
        public int DaysLate { get; set; }
    }
}