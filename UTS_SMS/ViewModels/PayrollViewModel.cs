 
using SMS.Models;

namespace SMS.ViewModels
{
    public class EmployeePayrollViewModel
    {
        public Employee Employee { get; set; }
        public PayrollMaster PayrollMaster { get; set; }
        public int TeacherAssignmentsCount { get; set; }
        public string RowColor { get; set; }
        public bool CanProcess { get; set; }
        public decimal SalaryDeductionsTotal { get; set; }
    }

    public class PayrollCreateViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeRole { get; set; }
        public int ForMonth { get; set; }
        public int ForYear { get; set; }

        // Employee details for dashboard-style display
        public Employee Employee { get; set; }
        public string EmployeeProfilePicture { get; set; }
        public string CampusName { get; set; }

        // Salary components
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal AttendanceDeduction { get; set; }
        public decimal Bonus { get; set; }
        public decimal AdvanceAdjustment { get; set; }
        public decimal PreviousBalance { get; set; }

        // Student fee-based salary deductions (for employee parents)
        public decimal StudentFeeDeduction { get; set; }
        public List<StudentFeeDeductionDetail> StudentFeeDeductions { get; set; } = new List<StudentFeeDeductionDetail>();
        public bool IsEmployeeParent { get; set; }

        // Calculated properties
        public decimal GrossSalary => BasicSalary + Allowances;
        public decimal NetSalary => GrossSalary - Deductions - AttendanceDeduction - StudentFeeDeduction + Bonus + AdvanceAdjustment + PreviousBalance;

        // Payment details
        public decimal AmountPaid { get; set; }
        public decimal CashPaid { get; set; }
        public decimal OnlinePaid { get; set; }
        public int? OnlineAccount { get; set; }
        public string TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceivedBy { get; set; }
        
        // Indicates if this is an existing payroll record with a balance
        public bool IsExistingRecordWithBalance { get; set; }
        public decimal ExistingBalance { get; set; }
        public decimal ExistingAmountPaid { get; set; }

        // Attendance records (using EmployeeAttendance directly)
        public List<EmployeeAttendanceViewModel> AttendanceRecords { get; set; } = new List<EmployeeAttendanceViewModel>();

        // Attendance trend data for chart
        public List<AttendanceChartData> AttendanceTrends { get; set; } = new List<AttendanceChartData>();

        // Multi-period attendance summaries (this month, last month, last year)
        public List<AttendancePeriodSummary> AttendancePeriodSummaries { get; set; } = new List<AttendancePeriodSummary>();

        // Salary deductions from database
        public List<SalaryDeductionItem> SalaryDeductions { get; set; } = new List<SalaryDeductionItem>();
        public decimal TotalSalaryDeductions { get; set; }

        // Teacher performance
        public TeacherPerformanceViewModel TeacherPerformance { get; set; }

        // Payroll history for employee record display
        public List<PayrollHistoryItem> PayrollHistory { get; set; } = new List<PayrollHistoryItem>();

        // Attendance summary stats
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLeave { get; set; }
        public int TotalLate { get; set; }
        public int TotalShortLeave { get; set; }
        public int TotalHolidays { get; set; }
        public double AttendancePercentage { get; set; }
        
        // Holidays for the month
        public List<CalendarEvent> MonthHolidays { get; set; } = new List<CalendarEvent>();
    }

    public class StudentFeeDeductionDetail
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public int ForMonth { get; set; }
        public int ForYear { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal AmountDeducted { get; set; }
        public string PaymentMode { get; set; }
    }

    public class AttendanceChartData
    {
        public string Date { get; set; }
        public string DayOfWeek { get; set; }
        public string Status { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public int Late { get; set; }
        public int ShortLeave { get; set; }
        public double Percentage { get; set; }
    }

    public class SalaryDeductionItem
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public decimal AmountDeducted { get; set; }
        public DateTime DeductionDate { get; set; }
    }

    public class AttendancePeriodSummary
    {
        public string Period { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int ShortLeave { get; set; }
        public int Leave { get; set; }
        public int TotalDays { get; set; }
        public double AttendancePercentage => TotalDays > 0 ? Math.Round((double)Present / TotalDays * 100, 1) : 0;
    }

    public class PayrollHistoryItem
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
        public bool BalanceTransferredToNextMonth { get; set; } // Indicates if balance was added to next month
        public string TransferredToMonthName { get; set; } // Name of the month where balance was added
    }

    public class EmployeeAttendanceViewModel
    {
        public DateTime Date { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string Status { get; set; }
        public decimal DailySalary { get; set; }
        public decimal DeductionAmount { get; set; }
        public string Remarks { get; set; }
    }

    public class TeacherPerformanceViewModel
    {
        public List<TeacherAssignment> Assignments { get; set; } = new List<TeacherAssignment>();
        public List<ExamMarks> ExamResults { get; set; } = new List<ExamMarks>();
        public decimal AveragePercentage { get; set; }
        public decimal PassPercentage { get; set; }
        public int TotalStudents { get; set; }
        public int TotalExams { get; set; }
        
        // For enhanced performance graph
        public List<ClassPerformanceData> ClassPerformances { get; set; } = new List<ClassPerformanceData>();
        public List<SubjectPerformanceData> SubjectPerformances { get; set; } = new List<SubjectPerformanceData>();
        public List<string> ExamCategories { get; set; } = new List<string>();
    }
    
    public class ClassPerformanceData
    {
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public decimal ClassAverage { get; set; }
        public List<SubjectAverageData> SubjectAverages { get; set; } = new List<SubjectAverageData>();
    }
    
    public class SubjectAverageData
    {
        public string SubjectName { get; set; }
        public decimal Average { get; set; }
        public int StudentCount { get; set; }
    }
    
    public class SubjectPerformanceData
    {
        public string SubjectName { get; set; }
        public List<ClassAverageData> ClassAverages { get; set; } = new List<ClassAverageData>();
        public decimal OverallAverage { get; set; }
    }
    
    public class ClassAverageData
    {
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public decimal Average { get; set; }
        public int StudentCount { get; set; }
    }

    public class PayrollDetailViewModel
    {
        public PayrollMaster PayrollMaster { get; set; }
        public List<EmployeeAttendance> AttendanceRecords { get; set; }
    }
}
 