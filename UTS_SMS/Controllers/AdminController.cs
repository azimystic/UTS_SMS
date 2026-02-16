using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    // Admin Dashboard Controller
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExtraChargeService _extraChargeService;
        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IExtraChargeService extraChargeService)
        {
            _context = context;
            _userManager = userManager;
            _extraChargeService = extraChargeService;
        }

        public async Task<IActionResult> Dashboard(int? campusId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            
            // Admin can only see their campus data
            var effectiveCampusId = userCampusId;
            
            var today = DateTime.Today;
            
            // Get today's fee collection
            var todayFeeQuery = _context.BillingTransactions
                .AsNoTracking()
                .Include(bt => bt.BillingMaster)
                .ThenInclude(bm => bm.Student)
                .Where(bt => bt.PaymentDate.Date == today);
            
            if (effectiveCampusId.HasValue && effectiveCampusId > 0)
                todayFeeQuery = todayFeeQuery.Where(bt => bt.BillingMaster.Student.CampusId == effectiveCampusId);
            
            var todayFees = await todayFeeQuery.ToListAsync();
            
            var model = new DashboardViewModel
            {
                TotalStudents = await GetStudentCount(effectiveCampusId),
                TotalEmployees = await GetEmployeeCount(effectiveCampusId),
                TotalTeachers = await GetTeacherCount(effectiveCampusId),
                TotalUsers = await _userManager.Users.CountAsync(u => u.IsActive),
                RecentStudents = await GetRecentStudents(effectiveCampusId, 5),
                RecentEmployees = await GetRecentEmployees(effectiveCampusId, 5),
                AttendanceSummary = await GetAttendanceSummary(effectiveCampusId, today),
                NamazAttendanceSummary = await GetNamazAttendanceSummary(effectiveCampusId, today),
                FinancialSummary = await GetFinancialSummary(effectiveCampusId, DateTime.Now.Month, DateTime.Now.Year),
                InquirySummary = await GetInquirySummary(effectiveCampusId),
                ComplaintSummary = await GetComplaintSummary(effectiveCampusId),
                RecentFees = await GetRecentFees(effectiveCampusId, 10),
                RecentPayroll = await GetRecentPayroll(effectiveCampusId, 10),
                ActiveDuties = await GetActiveDuties(effectiveCampusId, 10),
                TeacherOfMonth = await GetTeacherOfMonth(effectiveCampusId, DateTime.Now.Month, DateTime.Now.Year),
                ExamSummary = await GetExamSummary(effectiveCampusId, DateTime.Now.Month, DateTime.Now.Year),
                TestReturnsSummary = await GetTestReturnsSummary(effectiveCampusId, DateTime.Now.Month, DateTime.Now.Year),
                UpcomingBirthdays = await GetUpcomingBirthdays(effectiveCampusId, 30),
                ToDoList = await GetToDoList(currentUser?.Id ?? ""),
                TodayDiaries = await GetTodayDiaries(effectiveCampusId),
                TodayFeeReceived = todayFees.Sum(tf => tf.AmountPaid),
                TodayFeeTransactions = todayFees.Count(),
                IsOwnerDashboard = false,
                SelectedCampusId = effectiveCampusId
            };

            return View(model);
        }
        private async Task<int> GetStudentCount(int? campusId)
        {
            var query = _context.Students.Where(s => !s.HasLeft);
            if (campusId.HasValue && campusId > 0)
                query = query.Where(s => s.CampusId == campusId);
            return await query.CountAsync();
        }

        private async Task<int> GetEmployeeCount(int? campusId)
        {
            // Staff Count: Filter out anyone with designation/role of "Teacher"
            var query = _context.Employees.Where(e => e.IsActive && e.Role.ToLower() != "teacher");
            if (campusId.HasValue && campusId > 0)
                query = query.Where(e => e.CampusId == campusId);
            return await query.CountAsync();
        }

        private async Task<int> GetTeacherCount(int? campusId)
        {
            var query = _context.Employees.Where(e => e.IsActive && e.Role.ToLower() == "teacher");
            if (campusId.HasValue && campusId > 0)
                query = query.Where(e => e.CampusId == campusId);
            return await query.CountAsync();
        }

        private async Task<List<Student>> GetRecentStudents(int? campusId, int count)
        {
            var query = _context.Students.Where(s => !s.HasLeft);
            if (campusId.HasValue && campusId > 0)
                query = query.Where(s => s.CampusId == campusId);
            return await query.OrderByDescending(s => s.RegistrationDate).Take(count).ToListAsync();
        }

        private async Task<List<Employee>> GetRecentEmployees(int? campusId, int count)
        {
            var query = _context.Employees.Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
                query = query.Where(e => e.CampusId == campusId);
            return await query.OrderByDescending(e => e.JoiningDate).Take(count).ToListAsync();
        }

        private async Task<AttendanceSummary> GetAttendanceSummary(int? campusId, DateTime date)
        {
            var studentAttendanceQuery = _context.Attendance
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.Date == date);
            
            var employeeAttendanceQuery = _context.EmployeeAttendance
                .AsNoTracking()
                .Where(ea => ea.Date == date);

            if (campusId.HasValue && campusId > 0)
            {
                studentAttendanceQuery = studentAttendanceQuery.Where(a => a.Student.CampusId == campusId);
                employeeAttendanceQuery = employeeAttendanceQuery.Where(ea => ea.Employee.CampusId == campusId);
            }

            var studentAttendance = await studentAttendanceQuery.ToListAsync();
            var employeeAttendance = await employeeAttendanceQuery.ToListAsync();

            var studentPresent = studentAttendance.Count(a => a.Status == "P" || a.Status == "L");
            var studentTotal = studentAttendance.Count();
            var teacherPresent = employeeAttendance.Count(ea => ea.Status == "P" || ea.Status == "T" || ea.Status == "S");
            var teacherTotal = employeeAttendance.Count();

            return new AttendanceSummary
            {
                TotalStudentsPresent = studentPresent,
                TotalStudentsAbsent = studentTotal - studentPresent,
                TotalTeachersPresent = teacherPresent,
                TotalTeachersAbsent = teacherTotal - teacherPresent,
                StudentAttendancePercentage = studentTotal > 0 ? Math.Round((double)studentPresent / studentTotal * 100, 1) : 0,
                TeacherAttendancePercentage = teacherTotal > 0 ? Math.Round((double)teacherPresent / teacherTotal * 100, 1) : 0,
                Date = date,
                StudentAttendanceChart = await GetStudentAttendanceChartData(campusId, date),
                TeacherAttendanceChart = await GetTeacherAttendanceChartData(campusId, date)
            };
        }

        private async Task<List<ChartDataPoint>> GetStudentAttendanceChartData(int? campusId, DateTime date)
        {
            var query = _context.Attendance
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.Date == date);

            if (campusId.HasValue && campusId > 0)
                query = query.Where(a => a.Student.CampusId == campusId);

            var data = await query
                .GroupBy(a => a.Status)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key == "P" ? "Present" : g.Key == "A" ? "Absent" : g.Key == "L" ? "Late" : "Leave",
                    Value = g.Count(),
                    Color = g.Key == "P" ? "#10b981" : g.Key == "A" ? "#ef4444" : g.Key == "L" ? "#f59e0b" : "#6b7280"
                })
                .ToListAsync();

            return data;
        }

        private async Task<List<ChartDataPoint>> GetTeacherAttendanceChartData(int? campusId, DateTime date)
        {
            var query = _context.EmployeeAttendance
                .AsNoTracking()
                .Include(ea => ea.Employee)
                .Where(ea => ea.Date == date);

            if (campusId.HasValue && campusId > 0)
                query = query.Where(ea => ea.Employee.CampusId == campusId);

            var data = await query
                .GroupBy(ea => ea.Status)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key == "P" ? "Present" : 
                            g.Key == "A" ? "Absent" : 
                            g.Key == "L" ? "Leave" : 
                            g.Key == "T" ? "Late" : 
                            g.Key == "S" ? "Short Leave" : "Other",
                    Value = g.Count(),
                    Color = g.Key == "P" ? "#10b981" : 
                            g.Key == "A" ? "#ef4444" : 
                            g.Key == "L" ? "#f59e0b" : 
                            g.Key == "T" ? "#fb923c" : 
                            g.Key == "S" ? "#a78bfa" : "#6b7280"
                })
                .ToListAsync();

            return data;
        }

        private async Task<NamazAttendanceSummary> GetNamazAttendanceSummary(int? campusId, DateTime date)
        {
            var query = _context.NamazAttendance.AsNoTracking().Where(na => na.Date == date);
            
            if (campusId.HasValue && campusId > 0)
                query = query.Where(na => na.CampusId == campusId);

            var records = await query.ToListAsync();

            return new NamazAttendanceSummary
            {
                StudentsWithJamat = records.Count(r => r.StudentId.HasValue && r.Status == "WJ"),
                StudentsQaza = records.Count(r => r.StudentId.HasValue && r.Status == "QZ"),
                StudentsWithoutJamat = records.Count(r => r.StudentId.HasValue && r.Status == "WOJ"),
                EmployeesWithJamat = records.Count(r => r.EmployeeId.HasValue && r.Status == "WJ"),
                EmployeesQaza = records.Count(r => r.EmployeeId.HasValue && r.Status == "QZ"),
                EmployeesWithoutJamat = records.Count(r => r.EmployeeId.HasValue && r.Status == "WOJ"),
                Date = date
            };
        }

        private async Task<FinancialSummary> GetFinancialSummary(int? campusId, int month, int year)
        {
            var today = DateTime.Today;
            var isCurrentMonth = (month == today.Month && year == today.Year);
            
            // 1. Get actual revenue from billing transactions (Collected This Month)
            var revenueQuery = _context.BillingTransactions
                .AsNoTracking()
                .Include(bt => bt.BillingMaster)
                .ThenInclude(bm => bm.Student)
                .Where(bt => bt.PaymentDate.Month == month && bt.PaymentDate.Year == year);

            if (campusId.HasValue && campusId > 0)
                revenueQuery = revenueQuery.Where(bt => bt.BillingMaster.Student.CampusId == campusId);

            var actualRevenue = await revenueQuery.SumAsync(bt => bt.AmountPaid);

            // 2. Get expenses
            var expenseQuery = _context.Expenses.AsNoTracking().Where(e => e.Month == month && e.Year == year);
            if (campusId.HasValue && campusId > 0)
                expenseQuery = expenseQuery.Where(e => e.CampusId == campusId);

            var totalExpenses = await expenseQuery.SumAsync(e => e.Amount);

            // 3. Get salary expenses - Use PayrollMaster ForMonth/ForYear instead of payment date
            // Salaries are calculated based on the month they are intended for, not when paid
            var payrollQuery = _context.PayrollTransactions
                .AsNoTracking()
                .Include(pt => pt.PayrollMaster)
                .ThenInclude(pm => pm.Employee)
                .Where(pt => pt.PayrollMaster.ForMonth == month && pt.PayrollMaster.ForYear == year);

            if (campusId.HasValue && campusId > 0)
                payrollQuery = payrollQuery.Where(pt => pt.PayrollMaster.Employee.CampusId == campusId);

            var totalSalaries = await payrollQuery.SumAsync(pt => pt.AmountPaid);

            // 4. Get asset values
            var assetQuery = _context.Assets.AsNoTracking().Where(a => a.IsActive);
            if (campusId.HasValue && campusId > 0)
                assetQuery = assetQuery.Where(a => a.CampusId == campusId);

            var assetValue = await assetQuery.SumAsync(a => a.Price);

            // 5. ✅ IMPROVED: Calculate TARGET using proper billing logic
            var targetAmount = await CalculateTotalFeesForPeriod(campusId, month, year, month, year);

            var totalExpenditures = totalSalaries + totalExpenses;
            var profitLoss = actualRevenue - totalExpenditures;

            // Get campus name
            var campusName = "All Campuses";
            if (campusId.HasValue && campusId > 0)
            {
                var campus = await _context.Campuses.FindAsync(campusId);
                if (campus != null)
                    campusName = campus.Name;
            }

            return new FinancialSummary
            {
                MonthlyRevenueActual = actualRevenue,
                MonthlyRevenueExpected = targetAmount,
                RevenuePercentage = targetAmount > 0 ? Math.Round((double)(actualRevenue / targetAmount) * 100, 1) : 0,
                TotalSalariesPaid = totalSalaries,
                TotalExpenses = totalExpenses,
                AssetValue = assetValue,
                TotalExpenditures = totalExpenditures,
                ProfitLoss = profitLoss,
                ProfitLossPercentage = actualRevenue > 0 ? Math.Round((double)(profitLoss / actualRevenue) * 100, 1) : 0,
                TotalFeesToBeCollected = targetAmount,
                Month = month,
                Year = year,
                CampusName = campusName
            };
        }

        // ✅ NEW METHOD: Calculate total fees for a period using billing report logic
        private async Task<decimal> CalculateTotalFeesForPeriod(int? campusId, int startMonth, int startYear, int endMonth, int endYear)
        {
            // Get all active students who were enrolled during this period
            var studentsQuery = _context.Students
                .AsNoTracking()
                .Include(s => s.ClassObj)
                .Where(s => !s.HasLeft);

            if (campusId.HasValue && campusId > 0)
                studentsQuery = studentsQuery.Where(s => s.CampusId == campusId);

            var students = await studentsQuery.ToListAsync();
            
            decimal totalFeesExpected = 0;

            foreach (var student in students)
            {
                // Get billing records for this period
                var billingRecords = await _context.BillingMaster
                    .Include(b => b.Transactions)
                    .Where(b => b.StudentId == student.Id &&
                               ((b.ForYear > startYear) ||
                                (b.ForYear == startYear && b.ForMonth >= startMonth)) &&
                               ((b.ForYear < endYear) ||
                                (b.ForYear == endYear && b.ForMonth <= endMonth)))
                    .ToListAsync();

                if (billingRecords.Any())
                {
                    // ✅ Student has billing records - sum all payable amounts
                    totalFeesExpected += billingRecords.Sum(b => 
                        b.TuitionFee + b.AdmissionFee + b.Fine + b.PreviousDues + b.MiscallaneousCharges);
                }
                else
                {
                    // ✅ No billing record - calculate manually like in Create()
                    var classFee = await _context.ClassFees
                        .FirstOrDefaultAsync(cf => cf.ClassId == student.ClassObj.Id);

                    if (classFee == null) continue;

                    // Calculate tuition with discount
                    var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                    
                    // Calculate extra charges (monthly charges only for expected fees)
                    var extraChargesQuery = _context.ClassFeeExtraCharges
                        .Where(ec => ec.IsActive && 
                                    (ec.ClassId == student.Class || ec.ClassId == null) &&
                                    ec.Category == "MonthlyCharges");
            
                    if (campusId.HasValue && campusId > 0)
                        extraChargesQuery = extraChargesQuery.Where(ec => ec.CampusId == campusId);
            
                    var extraCharges = await _extraChargeService.CalculateExtraCharges(student.ClassObj.Id, student.Id, student.CampusId);
                    // Get unpaid fines
                    var unpaidFines = await _context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == student.Id && !sfc.IsPaid && sfc.IsActive)
                        .SumAsync(sfc => (decimal?)sfc.Amount) ?? 0;
                    
                    var misc = extraCharges + unpaidFines;

                    // Calculate admission fee with discount
                    var admissionFee = student.AdmissionFeePaid ? 0 :
                        Math.Max(0, classFee.AdmissionFee * (1 - ((student.AdmissionFeeDiscountAmount ?? 0) / 100m)));

                    // Get previous dues from last billing
                    var lastRecord = await _context.BillingMaster
                        .Where(b => b.StudentId == student.Id)
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefaultAsync();

                    var previousDues = lastRecord?.Dues ?? 0;

                    // Calculate number of months in period
                    var monthsInPeriod = ((endYear - startYear) * 12) + (endMonth - startMonth) + 1;
                    
                    // Total expected = (tuition + misc) * months + admission + previous dues
                    totalFeesExpected += (tuitionFee + misc) * monthsInPeriod + admissionFee + previousDues;
                }
            }

            return totalFeesExpected;
        }

        private async Task<InquirySummary> GetInquirySummary(int? campusId)
        {
            var query = _context.AdmissionInquiries
                .AsNoTracking()
                .Include(ai => ai.ClassInterested)
                .Where(ai => ai.IsActive);
            
            if (campusId.HasValue && campusId > 0)
                query = query.Where(ai => ai.CampusId == campusId);

            var inquiries = await query.ToListAsync();

            var totalInquiries = inquiries.Count;
            var admissionsTaken = inquiries.Count(i => i.InquiryStatus == "Enrolled");
            var pending = inquiries.Count(i => i.InquiryStatus == "New" || i.InquiryStatus == "Contacted" || i.InquiryStatus == "Visited");
            var rejected = inquiries.Count(i => i.InquiryStatus == "Rejected");

            // This Month calculations
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
            var thisMonthInquiries = inquiries.Where(i => i.InquiryDate >= thisMonthStart && i.InquiryDate <= thisMonthEnd).ToList();

            // Last Month calculations
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);
            var lastMonthInquiries = inquiries.Where(i => i.InquiryDate >= lastMonthStart && i.InquiryDate <= lastMonthEnd).ToList();

            return new InquirySummary
            {
                TotalInquiries = totalInquiries,
                AdmissionsTaken = admissionsTaken,
                PendingInquiries = pending,
                RejectedInquiries = rejected,
                AdmissionRate = totalInquiries > 0 ? Math.Round((double)admissionsTaken / totalInquiries * 100, 1) : 0,
                ThisMonthTotal = thisMonthInquiries.Count,
                ThisMonthEnrolled = thisMonthInquiries.Count(i => i.InquiryStatus == "Enrolled"),
                ThisMonthRejected = thisMonthInquiries.Count(i => i.InquiryStatus == "Rejected"),
                ThisMonthInquiries = thisMonthInquiries.Select(i => new AdmissionInquiryDetail
                {
                    Id = i.Id,
                    StudentName = i.StudentName,
                    PhoneNumber = i.PhoneNumber,
                    ClassName = i.ClassInterested?.Name ?? "",
                    InquiryStatus = i.InquiryStatus,
                    InquiryDate = i.InquiryDate
                }).OrderByDescending(i => i.InquiryDate).ToList(),
                LastMonthTotal = lastMonthInquiries.Count,
                LastMonthEnrolled = lastMonthInquiries.Count(i => i.InquiryStatus == "Enrolled"),
                LastMonthRejected = lastMonthInquiries.Count(i => i.InquiryStatus == "Rejected"),
                LastMonthInquiries = lastMonthInquiries.Select(i => new AdmissionInquiryDetail
                {
                    Id = i.Id,
                    StudentName = i.StudentName,
                    PhoneNumber = i.PhoneNumber,
                    ClassName = i.ClassInterested?.Name ?? "",
                    InquiryStatus = i.InquiryStatus,
                    InquiryDate = i.InquiryDate
                }).OrderByDescending(i => i.InquiryDate).ToList()
            };
        }

        private async Task<ComplaintSummary> GetComplaintSummary(int? campusId)
        {
            var query = _context.StudentComplaints
                .AsNoTracking()
                .Include(sc => sc.Student)
                .Where(sc => sc.IsActive);
            
            if (campusId.HasValue && campusId > 0)
                query = query.Where(sc => sc.CampusId == campusId);

            var complaints = await query.ToListAsync();
            
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            // Filter for Admin/Teacher complaints
            var adminTeacherComplaints = complaints
                .Where(c => c.ReporterType == "Admin" || c.ReporterType == "Teacher")
                .Select(c => new ComplaintDetail
                {
                    Id = c.Id,
                    ComplaintTitle = c.ComplaintTitle,
                    StudentName = c.Student?.StudentName ?? "N/A",
                    Status = c.Status,
                    ReporterType = c.ReporterType ?? "N/A",
                    ComplaintDate = c.ComplaintDate,
                    IsToday = c.ComplaintDate.Date == today,
                    IsYesterday = c.ComplaintDate.Date == yesterday
                })
                .OrderByDescending(c => c.ComplaintDate)
                .Take(10)
                .ToList();

            // Filter for Student/Parent complaints
            var studentParentComplaints = complaints
                .Where(c => c.ReporterType == "Student" || c.ReporterType == "Parent")
                .Select(c => new ComplaintDetail
                {
                    Id = c.Id,
                    ComplaintTitle = c.ComplaintTitle,
                    StudentName = c.Student?.StudentName ?? "N/A",
                    Status = c.Status,
                    ReporterType = c.ReporterType ?? "N/A",
                    ComplaintDate = c.ComplaintDate,
                    IsToday = c.ComplaintDate.Date == today,
                    IsYesterday = c.ComplaintDate.Date == yesterday
                })
                .OrderByDescending(c => c.ComplaintDate)
                .Take(10)
                .ToList();

            return new ComplaintSummary
            {
                TotalPendingComplaints = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed"),
                OpenComplaints = complaints.Count(c => c.Status == "Open"),
                InvestigationComplaints = complaints.Count(c => c.Status == "In Investigation"),
                EscalatedComplaints = complaints.Count(c => c.Status == "Escalated"),
                AdminTeacherComplaints = adminTeacherComplaints,
                StudentParentComplaints = studentParentComplaints
            };
        }

        private async Task<List<BillingTransaction>> GetRecentFees(int? campusId, int count)
        {
            IQueryable<BillingTransaction> query = _context.BillingTransactions
                .AsNoTracking()
                .Include(bt => bt.BillingMaster)
                .ThenInclude(bm => bm.Student);

            if (campusId.HasValue && campusId > 0)
                query = query.Where(bt => bt.BillingMaster.Student.CampusId == campusId);

            return await query.OrderByDescending(bt => bt.PaymentDate).Take(count).ToListAsync();
        }

        private async Task<List<PayrollTransaction>> GetRecentPayroll(int? campusId, int count)
        {
            IQueryable<PayrollTransaction> query = _context.PayrollTransactions
                .AsNoTracking()
                .Include(pt => pt.PayrollMaster)
                .ThenInclude(pm => pm.Employee);

            if (campusId.HasValue && campusId > 0)
                query = query.Where(pt => pt.PayrollMaster.Employee.CampusId == campusId);

            return await query.OrderByDescending(pt => pt.PaymentDate).Take(count).ToListAsync();
        }

        private async Task<List<AssignedDuty>> GetActiveDuties(int? campusId, int count)
        {
            var today = DateTime.Today;
            
            var query = _context.AssignedDuties
                .AsNoTracking()
                .Include(ad => ad.Employee)
                .Include(ad => ad.AssignedStudents)
                    .ThenInclude(ads => ads.Student)
                .Where(ad => ad.IsActive && 
                      ad.Status != "Completed" && 
                      ad.Status != "Cancelled" &&
                      ad.StartDate <= today &&
                      ad.DueDate >= today);

            if (campusId.HasValue && campusId > 0)
                query = query.Where(ad => ad.CampusId == campusId);

            return await query.OrderByDescending(ad => ad.AssignedDate).Take(count).ToListAsync();
        }

        private async Task<TeacherOfMonthViewModel?> GetTeacherOfMonth(int? campusId, int month, int year)
        {
            // Get previous month's data
            var previousMonth = new DateTime(year, month, 1).AddMonths(-1);
            var targetMonth = previousMonth.Month;
            var targetYear = previousMonth.Year;
            
            var query = _context.TeacherPerformances
                .AsNoTracking()
                .Include(tp => tp.Teacher)
                .Where(tp => tp.Month == targetMonth && tp.Year == targetYear && tp.IsActive);

            if (campusId.HasValue && campusId > 0)
                query = query.Where(tp => tp.CampusId == campusId);

            var topPerformance = await query.OrderByDescending(tp => tp.TotalScore).FirstOrDefaultAsync();

            if (topPerformance == null)
                return null;

            return new TeacherOfMonthViewModel
            {
                Teacher = topPerformance.Teacher,
                Score = topPerformance.TotalScore,
                Achievements = $"Scored {topPerformance.TotalScore:F1}/20.0",
                Month = targetMonth,
                Year = targetYear
            };
        }

        private async Task<ExamSummary> GetExamSummary(int? campusId, int month, int year)
        {
            var examQuery = _context.Exams.AsNoTracking().Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
                examQuery = examQuery.Where(e => e.CampusId == campusId);

            var totalExamsScheduled = await examQuery.CountAsync();

            // Get exam marks data for the current month/year
            var examMarksQuery = _context.ExamMarks
                .AsNoTracking()
                .Include(em => em.Exam)
                .ThenInclude(e => e.ExamCategory)
                .Include(em => em.Student)
                .Where(em => em.IsActive && em.ExamDate.Month == month && em.ExamDate.Year == year);

            if (campusId.HasValue && campusId > 0)
                examMarksQuery = examMarksQuery.Where(em => em.CampusId == campusId);

            var examMarks = await examMarksQuery.ToListAsync();

            var totalStudentExamEntries = examMarks.Count();
            var passedStudents = examMarks.Count(em => em.Status != "Fail");
            var failedStudents = totalStudentExamEntries - passedStudents;
            var overallPassPercentage = totalStudentExamEntries > 0 ? Math.Round((double)passedStudents / totalStudentExamEntries * 100, 1) : 0;
            var averageMarksPercentage = examMarks.Any() ? Math.Round(examMarks.Average(em => (double)em.Percentage), 1) : 0;

            var examCategoryStats = examMarks
                .GroupBy(em => em.Exam.ExamCategory.Name)
                .Select(g => new ExamCategoryStats
                {
                    CategoryName = g.Key,
                    ExamsCount = g.Select(em => em.ExamId).Distinct().Count(),
                    StudentEntries = g.Count(),
                    PassPercentage = g.Count() > 0 ? Math.Round((double)g.Count(em => em.Status != "Fail") / g.Count() * 100, 1) : 0
                })
                .ToList();

            var recentResults = examMarks
                .GroupBy(em => new { em.ExamId, em.Exam.Name, CategoryName = em.Exam.ExamCategory.Name, em.ExamDate })
                .Select(g => new RecentExamResult
                {
                    ExamId = g.Key.ExamId,
                    ExamName = g.Key.Name,
                    CategoryName = g.Key.CategoryName,
                    ExamDate = g.Key.ExamDate,
                    StudentsParticipated = g.Count(),
                    PassPercentage = g.Count() > 0 ? Math.Round((double)g.Count(em => em.Status != "Fail") / g.Count() * 100, 1) : 0
                })
                .OrderByDescending(r => r.ExamDate)
                .Take(5)
                .ToList();

            var completedExams = examMarks.Select(em => em.ExamId).Distinct().Count();
            var examsPending = Math.Max(0, totalExamsScheduled - completedExams);

            return new ExamSummary
            {
                TotalExamsScheduled = totalExamsScheduled,
                ExamsCompleted = completedExams,
                ExamsInProgress = 0,
                ExamsPending = examsPending,
                TotalStudentExamEntries = totalStudentExamEntries,
                PassedStudents = passedStudents,
                FailedStudents = failedStudents,
                OverallPassPercentage = overallPassPercentage,
                AverageMarksPercentage = averageMarksPercentage,
                ExamCategoryStats = examCategoryStats,
                RecentResults = recentResults,
                LastUpdated = DateTime.Now
            };
        }

        private async Task<TestReturnsSummary> GetTestReturnsSummary(int? campusId, int month, int year)
        {
            var testReturnsQuery = _context.TestReturns
                .AsNoTracking()
                .Include(tr => tr.Exam)
                .Include(tr => tr.Teacher)
                .Include(tr => tr.Subject)
                .Where(tr => tr.IsActive && tr.ExamDate.Month == month && tr.ExamDate.Year == year);

            if (campusId.HasValue && campusId > 0)
                testReturnsQuery = testReturnsQuery.Where(tr => tr.CampusId == campusId);

            var testReturns = await testReturnsQuery.ToListAsync();

            var totalTestsScheduled = testReturns.Count();
            var testsReturnedOnTime = testReturns.Count(tr => tr.IsReturnedOnTime);
            var testsReturnedLate = testReturns.Count(tr => tr.ReturnDate.HasValue && !tr.IsReturnedOnTime);
            var testsPendingReturn = testReturns.Count(tr => !tr.ReturnDate.HasValue);

            var onTimeReturnPercentage = totalTestsScheduled > 0 ? Math.Round((double)testsReturnedOnTime / totalTestsScheduled * 100, 1) : 0;

            var testsWithGoodChecking = testReturns.Count(tr => tr.CheckingQuality == "Good");
            var testsWithBetterChecking = testReturns.Count(tr => tr.CheckingQuality == "Better");
            var testsWithBadChecking = testReturns.Count(tr => tr.CheckingQuality == "Bad");

            var teacherPerformance = testReturns
                .GroupBy(tr => new { tr.TeacherId, tr.Teacher.FullName })
                .Select(g => new TeacherTestPerformance
                {
                    TeacherId = g.Key.TeacherId,
                    TeacherName = g.Key.FullName,
                    TestsAssigned = g.Count(),
                    TestsReturned = g.Count(tr => tr.ReturnDate.HasValue),
                    OnTimeReturns = g.Count(tr => tr.IsReturnedOnTime),
                    OnTimePercentage = g.Count() > 0 ? Math.Round((double)g.Count(tr => tr.IsReturnedOnTime) / g.Count() * 100, 1) : 0,
                    AverageCheckingQuality = g.GroupBy(tr => tr.CheckingQuality).OrderByDescending(cq => cq.Count()).FirstOrDefault()?.Key ?? "N/A"
                })
                .OrderByDescending(tp => tp.OnTimePercentage)
                .Take(5)
                .ToList();

            var recentReturns = testReturns
                .OrderByDescending(tr => tr.ReturnDate ?? tr.ExamDate)
                .Take(5)
                .Select(tr => new RecentTestReturn
                {
                    TestReturnId = tr.Id,
                    ExamName = tr.Exam.Name,
                    SubjectName = tr.Subject.Name,
                    TeacherName = tr.Teacher.FullName,
                    ExamDate = tr.ExamDate,
                    ReturnDate = tr.ReturnDate,
                    IsReturnedOnTime = tr.IsReturnedOnTime,
                    CheckingQuality = tr.CheckingQuality,
                    DaysLate = tr.ReturnDate.HasValue && !tr.IsReturnedOnTime ? 
                               Math.Max(0, (int)(tr.ReturnDate.Value - tr.ExamDate.AddDays(3)).TotalDays) : 0
                })
                .ToList();

            return new TestReturnsSummary
            {
                TotalTestsScheduled = totalTestsScheduled,
                TestsReturnedOnTime = testsReturnedOnTime,
                TestsReturnedLate = testsReturnedLate,
                TestsPendingReturn = testsPendingReturn,
                OnTimeReturnPercentage = onTimeReturnPercentage,
                TestsWithGoodChecking = testsWithGoodChecking,
                TestsWithBetterChecking = testsWithBetterChecking,
                TestsWithBadChecking = testsWithBadChecking,
                TeacherPerformance = teacherPerformance,
                RecentReturns = recentReturns,
                LastUpdated = DateTime.Now
            };
        }

        // API Endpoints for real-time dashboard data
        [HttpGet]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var today = DateTime.Today;

            var metrics = new
            {
                totalStudents = await GetStudentCount(campusId),
                totalEmployees = await GetEmployeeCount(campusId),
                totalTeachers = await GetTeacherCount(campusId),
                totalUsers = await _userManager.Users.CountAsync(u => u.IsActive),
                attendanceSummary = await GetAttendanceSummary(campusId, today),
                financialSummary = await GetFinancialSummary(campusId, DateTime.Now.Month, DateTime.Now.Year)
            };

            return Json(metrics);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceTrends(int days = 30)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days);

            // Get holidays in this date range
            var holidaysQuery = _context.CalendarEvents
                .AsNoTracking()
                .Where(ce => ce.IsActive && ce.IsHoliday && 
                            ce.StartDate >= startDate && ce.StartDate <= endDate);
            
            if (campusId.HasValue && campusId > 0)
                holidaysQuery = holidaysQuery.Where(ce => ce.CampusId == campusId);
            
            var holidayDates = await holidaysQuery
                .Select(ce => ce.StartDate.Date)
                .ToListAsync();

            var attendanceData = new List<object>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Skip Sundays and holidays
                if (date.DayOfWeek == DayOfWeek.Sunday || holidayDates.Contains(date.Date))
                    continue;
                    
                var studentAttendanceQuery = _context.Attendance
                    .AsNoTracking()
                    .Include(a => a.Student)
                    .Where(a => a.Date == date);

                if (campusId.HasValue && campusId > 0)
                    studentAttendanceQuery = studentAttendanceQuery.Where(a => a.Student.CampusId == campusId);

                var studentAttendance = await studentAttendanceQuery.ToListAsync();
                var studentPresent = studentAttendance.Count(a => a.Status == "P" || a.Status == "L");
                var studentTotal = studentAttendance.Count();

                attendanceData.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    dayOfWeek = date.ToString("ddd"),
                    present = studentPresent,
                    absent = studentTotal - studentPresent,
                    total = studentTotal,
                    percentage = studentTotal > 0 ? Math.Round((double)studentPresent / studentTotal * 100, 1) : 0
                });
            }

            return Json(attendanceData);
        }

        [HttpGet]
        public async Task<IActionResult> GetFeeCollectionTrends(int months = 6, int days = 0)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var feeData = new List<object>();

            if (days > 0)
            {
                // Daily view - for daily, we calculate monthly target divided by days in month
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-days);
                
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var revenueQuery = _context.BillingTransactions
                        .AsNoTracking()
                        .Include(bt => bt.BillingMaster)
                        .ThenInclude(bm => bm.Student)
                        .Where(bt => bt.PaymentDate.Date == date);

                    if (campusId.HasValue && campusId > 0)
                        revenueQuery = revenueQuery.Where(bt => bt.BillingMaster.Student.CampusId == campusId);

                    var totalCollected = await revenueQuery.SumAsync(bt => bt.AmountPaid);
                    var transactionCount = await revenueQuery.CountAsync();

                    feeData.Add(new
                    {
                        date = date.ToString("MMM dd"),
                        collected = totalCollected,
                        transactions = transactionCount,
                        target = 0 // Daily target not calculated for simplicity
                    });
                }
            }
            else
            {
                // ✅ FIXED: Monthly view with proper fee calculation
                var endDate = DateTime.Today;
                var startDate = endDate.AddMonths(-months);

                for (var date = startDate; date <= endDate; date = date.AddMonths(1))
                {
                    var month = date.Month;
                    var year = date.Year;

                    var revenueQuery = _context.BillingTransactions
                        .AsNoTracking()
                        .Include(bt => bt.BillingMaster)
                        .ThenInclude(bm => bm.Student)
                        .Where(bt => bt.PaymentDate.Month == month && bt.PaymentDate.Year == year);

                    if (campusId.HasValue && campusId > 0)
                        revenueQuery = revenueQuery.Where(bt => bt.BillingMaster.Student.CampusId == campusId);

                    var totalCollected = await revenueQuery.SumAsync(bt => bt.AmountPaid);
                    var transactionCount = await revenueQuery.CountAsync();

                    // ✅ Use the new calculation method for target
                    var monthTarget = await CalculateTotalFeesForPeriod(campusId, month, year, month, year);

                    feeData.Add(new
                    {
                        month = date.ToString("MMM yyyy"),
                        collected = totalCollected,
                        transactions = transactionCount,
                        target = monthTarget
                    });
                }
            }

            return Json(feeData);
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffMetrics()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var query = _context.Employees.AsNoTracking().Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
                query = query.Where(e => e.CampusId == campusId);

            var employees = await query.ToListAsync();

            var roleDistribution = employees
                .GroupBy(e => e.Role ?? "Unassigned")
                .Select(g => new
                {
                    role = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(r => r.count)
                .ToList();

            var today = DateTime.Today;
            var staffAttendanceQuery = _context.EmployeeAttendance
                .AsNoTracking()
                .Where(ea => ea.Date == today);

            if (campusId.HasValue && campusId > 0)
                staffAttendanceQuery = staffAttendanceQuery.Where(ea => ea.Employee.CampusId == campusId);

            var staffAttendance = await staffAttendanceQuery.ToListAsync();
            var staffPresent = staffAttendance.Count(ea => ea.Status == "P" || ea.Status == "L");

            return Json(new
            {
                totalStaff = employees.Count,
                roleDistribution = roleDistribution,
                todayPresent = staffPresent,
                todayAbsent = staffAttendance.Count - staffPresent,
                attendancePercentage = staffAttendance.Count > 0 ? 
                    Math.Round((double)staffPresent / staffAttendance.Count * 100, 1) : 0
            });
        }
        
        private async Task<List<BirthdayInfo>> GetUpcomingBirthdays(int? campusId, int days)
        {
            var today = DateTime.Today;
            var birthdays = new List<BirthdayInfo>();
            
            // Get student birthdays
            var studentQuery = _context.Students.AsNoTracking().Where(s => !s.HasLeft);
            if (campusId.HasValue && campusId > 0)
                studentQuery = studentQuery.Where(s => s.CampusId == campusId);
                
            var students = await studentQuery.ToListAsync();
            
            foreach (var student in students)
            {
                var nextBirthday = new DateTime(today.Year, student.DateOfBirth.Month, student.DateOfBirth.Day);
                if (nextBirthday < today)
                    nextBirthday = nextBirthday.AddYears(1);
                    
                var daysUntil = (nextBirthday - today).Days;
                if (daysUntil <= days)
                {
                    birthdays.Add(new BirthdayInfo
                    {
                        Id = student.Id,
                        Name = student.StudentName,
                        Type = "Student",
                        DateOfBirth = student.DateOfBirth,
                        DaysUntil = daysUntil
                    });
                }
            }
            
            // Get employee birthdays
            var employeeQuery = _context.Employees.AsNoTracking().Where(e => e.IsActive);
            if (campusId.HasValue && campusId > 0)
                employeeQuery = employeeQuery.Where(e => e.CampusId == campusId);
                
            var employees = await employeeQuery.ToListAsync();
            
            foreach (var employee in employees)
            {
                var nextBirthday = new DateTime(today.Year, employee.DateOfBirth.Month, employee.DateOfBirth.Day);
                if (nextBirthday < today)
                    nextBirthday = nextBirthday.AddYears(1);
                    
                var daysUntil = (nextBirthday - today).Days;
                if (daysUntil <= days)
                {
                    birthdays.Add(new BirthdayInfo
                    {
                        Id = employee.Id,
                        Name = employee.FullName,
                        Type = "Employee",
                        DateOfBirth = employee.DateOfBirth,
                        DaysUntil = daysUntil
                    });
                }
            }
            
            return birthdays.OrderBy(b => b.DaysUntil).Take(10).ToList();
        }
        
        private async Task<List<ToDoItem>> GetToDoList(string userId)
        {
            var todos = await _context.ToDos
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.IsActive)
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .Take(10)
                .Select(t => new ToDoItem
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description ?? string.Empty,
                    IsCompleted = t.IsCompleted,
                    DueDate = t.DueDate,
                    Priority = t.Priority
                })
                .ToListAsync();
            
            return todos;
        }
        
        private async Task<List<DiaryInfo>> GetTodayDiaries(int? campusId)
        {
            var today = DateTime.Today;
            var query = _context.Diaries
                .AsNoTracking()
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Class)
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Section)
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Where(d => d.Date == today);
            
            if (campusId.HasValue && campusId > 0)
                query = query.Where(d => d.CampusId == campusId);
            
            var diaries = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DiaryInfo
                {
                    Id = d.Id,
                    ClassName = d.TeacherAssignment.Class.Name,
                    SectionName = d.TeacherAssignment.Section.Name,
                    SubjectName = d.TeacherAssignment.Subject.Name,
                    TeacherName = d.TeacherAssignment.Teacher.FullName,
                    Homework = d.HomeworkGiven,
                    Date = d.Date
                })
                .ToListAsync();
            
            return diaries;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetRevenueTrends(string period = "thisMonth")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var revenueData = new List<object>();
            
            var today = DateTime.Today;
            DateTime startDate, endDate;
            
            switch (period.ToLower())
            {
                case "lastmonth":
                    startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    endDate = new DateTime(today.Year, today.Month, 1).AddDays(-1);
                    break;
                case "thisyear":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = today;
                    break;
                case "lastyear":
                    startDate = new DateTime(today.Year - 1, 1, 1);
                    endDate = new DateTime(today.Year - 1, 12, 31);
                    break;
                default: // thisMonth
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = today;
                    break;
            }
            
            // Group by month for year views, by day for month views
            var isYearlyView = period.ToLower().Contains("year");
            
            if (isYearlyView)
            {
                // Monthly aggregation for year views
                for (var date = startDate; date <= endDate; date = date.AddMonths(1))
                {
                    var month = date.Month;
                    var year = date.Year;
                    var monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    
                    var summary = await GetFinancialSummary(campusId, month, year);
                    
                    revenueData.Add(new
                    {
                        date = date.ToString("MMM yyyy"),
                        totalExpenses = summary.TotalExpenses,
                        totalSalaries = summary.TotalSalariesPaid,
                        totalFees = summary.MonthlyRevenueActual,
                        totalRevenue = summary.MonthlyRevenueActual - summary.TotalExpenditures
                    });
                }
            }
            else
            {
                // Daily aggregation for month views
                var daysInPeriod = (endDate - startDate).Days + 1;
                var dailyExpenses = new decimal[daysInPeriod];
                var dailySalaries = new decimal[daysInPeriod];
                var dailyFees = new decimal[daysInPeriod];
                
                // Get all transactions for the period
                var feesQuery = _context.BillingTransactions
                    .AsNoTracking()
                    .Include(bt => bt.BillingMaster)
                    .ThenInclude(bm => bm.Student)
                    .Where(bt => bt.PaymentDate >= startDate && bt.PaymentDate <= endDate);
                    
                if (campusId.HasValue && campusId > 0)
                    feesQuery = feesQuery.Where(bt => bt.BillingMaster.Student.CampusId == campusId);
                    
                var fees = await feesQuery.ToListAsync();
                
                var salariesQuery = _context.PayrollTransactions
                    .AsNoTracking()
                    .Include(pt => pt.PayrollMaster)
                    .ThenInclude(pm => pm.Employee)
                    .Where(pt => pt.PaymentDate >= startDate && pt.PaymentDate <= endDate);
                    
                if (campusId.HasValue && campusId > 0)
                    salariesQuery = salariesQuery.Where(pt => pt.PayrollMaster.Employee.CampusId == campusId);
                    
                var salaries = await salariesQuery.ToListAsync();
                
                var expensesQuery = _context.Expenses
                    .AsNoTracking()
                    .Where(e => e.CreatedDate >= startDate && e.CreatedDate <= endDate);
                    
                if (campusId.HasValue && campusId > 0)
                    expensesQuery = expensesQuery.Where(e => e.CampusId == campusId);
                    
                var expenses = await expensesQuery.ToListAsync();
                
                // Aggregate by day
                for (int i = 0; i < daysInPeriod; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    
                    dailyFees[i] = fees.Where(f => f.PaymentDate.Date == currentDate).Sum(f => f.AmountPaid);
                    dailySalaries[i] = salaries.Where(s => s.PaymentDate.Date == currentDate).Sum(s => s.AmountPaid);
                    dailyExpenses[i] = expenses.Where(e => e.CreatedDate.Date == currentDate).Sum(e => e.Amount);
                    
                    revenueData.Add(new
                    {
                        date = currentDate.ToString("MMM dd"),
                        totalExpenses = dailyExpenses[i],
                        totalSalaries = dailySalaries[i],
                        totalFees = dailyFees[i],
                        totalRevenue = dailyFees[i] - (dailyExpenses[i] + dailySalaries[i])
                    });
                }
            }
            
            return Json(revenueData);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetFinancialSummaryByPeriod(string period = "thisMonth", int? campusId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var effectiveCampusId = campusId ?? currentUser?.CampusId;
            
            var today = DateTime.Today;
            int month, year;
            
            switch (period.ToLower())
            {
                case "lastmonth":
                    var lastMonth = today.AddMonths(-1);
                    month = lastMonth.Month;
                    year = lastMonth.Year;
                    break;
                case "thisyear":
                    month = today.Month;
                    year = today.Year;
                    // ✅ FIXED: For year view, aggregate all months properly
                    var yearSummary = new FinancialSummary();
            
                    // Get actual revenue for the year
                    var yearRevenueQuery = _context.BillingTransactions
                .AsNoTracking()
                .Include(bt => bt.BillingMaster)
                .ThenInclude(bm => bm.Student)
                .Where(bt => bt.PaymentDate.Year == year && bt.PaymentDate.Month <= today.Month);
            
            if (effectiveCampusId.HasValue && effectiveCampusId > 0)
                yearRevenueQuery = yearRevenueQuery.Where(bt => bt.BillingMaster.Student.CampusId == effectiveCampusId);
            
            yearSummary.MonthlyRevenueActual = await yearRevenueQuery.SumAsync(bt => bt.AmountPaid);
            
            // Get expenses for the year
            var yearExpenseQuery = _context.Expenses
                .AsNoTracking()
                .Where(e => e.Year == year && e.Month <= today.Month);
            
            if (effectiveCampusId.HasValue && effectiveCampusId > 0)
                yearExpenseQuery = yearExpenseQuery.Where(e => e.CampusId == effectiveCampusId);
            
            yearSummary.TotalExpenses = await yearExpenseQuery.SumAsync(e => e.Amount);
            
            // Get salaries for the year - Use ForMonth/ForYear instead of PaymentDate
            var yearPayrollQuery = _context.PayrollTransactions
                .AsNoTracking()
                .Include(pt => pt.PayrollMaster)
                .ThenInclude(pm => pm.Employee)
                .Where(pt => pt.PayrollMaster.ForYear == year && pt.PayrollMaster.ForMonth <= today.Month);
            
            if (effectiveCampusId.HasValue && effectiveCampusId > 0)
                yearPayrollQuery = yearPayrollQuery.Where(pt => pt.PayrollMaster.Employee.CampusId == effectiveCampusId);
            
            yearSummary.TotalSalariesPaid = await yearPayrollQuery.SumAsync(pt => pt.AmountPaid);
            
            // ✅ Calculate expected fees for the entire year period (Jan to current month)
            yearSummary.TotalFeesToBeCollected = await CalculateTotalFeesForPeriod(
                effectiveCampusId, 1, year, today.Month, year);
            
            yearSummary.MonthlyRevenueExpected = yearSummary.TotalFeesToBeCollected;
            yearSummary.TotalExpenditures = yearSummary.TotalExpenses + yearSummary.TotalSalariesPaid;
            yearSummary.ProfitLoss = yearSummary.MonthlyRevenueActual - yearSummary.TotalExpenditures;
            yearSummary.RevenuePercentage = yearSummary.TotalFeesToBeCollected > 0 ? 
                Math.Round((double)(yearSummary.MonthlyRevenueActual / yearSummary.TotalFeesToBeCollected) * 100, 1) : 0;
            yearSummary.ProfitLossPercentage = yearSummary.MonthlyRevenueActual > 0 ? 
                Math.Round((double)(yearSummary.ProfitLoss / yearSummary.MonthlyRevenueActual) * 100, 1) : 0;
            yearSummary.Month = today.Month;
            yearSummary.Year = year;
            yearSummary.CampusName = await GetCampusName(effectiveCampusId);
            
            return Json(yearSummary);
        default: // thisMonth
            month = today.Month;
            year = today.Year;
            break;
    }
    
    var summary = await GetFinancialSummary(effectiveCampusId, month, year);
    return Json(summary);
}
        
        private async Task<string> GetCampusName(int? campusId)
        {
            if (!campusId.HasValue || campusId <= 0)
                return "All Campuses";
                
            var campus = await _context.Campuses.FindAsync(campusId);
            return campus?.Name ?? "Unknown Campus";
        }
        
        [HttpGet]
        public async Task<IActionResult> GetStudentCountTrends(int months = 6)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var studentCountData = new List<object>();
            
            var today = DateTime.Today;
            var startDate = today.AddMonths(-months);
            
            for (var date = startDate; date <= today; date = date.AddMonths(1))
            {
                var month = date.Month;
                var year = date.Year;
                var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                
                // Count students who were enrolled at the end of this month
                // Includes students who: 
                // - Were registered before or during this month
                // - Either haven't left OR left after this month
                var query = _context.Students
                    .AsNoTracking()
                    .Where(s => s.RegistrationDate <= lastDayOfMonth &&
                               (!s.HasLeft || (s.LeftDate.HasValue && s.LeftDate.Value > lastDayOfMonth)));
                
                if (campusId.HasValue && campusId > 0)
                    query = query.Where(s => s.CampusId == campusId);
                
                var count = await query.CountAsync();
                
                studentCountData.Add(new
                {
                    month = date.ToString("MMM yyyy"),
                    count = count
                });
            }
            
            return Json(studentCountData);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(int year, int month)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            // Get calendar events
            var eventsQuery = _context.CalendarEvents
                .AsNoTracking()
                .Where(ce => ce.IsActive && 
                            ce.StartDate >= startDate && 
                            ce.StartDate <= endDate);
            
            if (campusId.HasValue && campusId > 0)
                eventsQuery = eventsQuery.Where(ce => ce.CampusId == campusId);

            var calendarEvents = await eventsQuery
    .Select(ce => new
    {
        id = ce.Id,
        date = ce.StartDate.ToString("yyyy-MM-dd"),
        title = ce.EventName,
        description = ce.Description,
        classSection = "", // Add this line to match the examDates structure
        isHoliday = ce.IsHoliday,
        type = "event"
    })
    .ToListAsync();

            // Get exam dates from datesheet with class and section info
            var examDatesQuery = _context.ExamDateSheets
                .AsNoTracking()
                .Include(eds => eds.Exam)
                .Include(eds => eds.Subject)
                .Include(eds => eds.ClassSections)
                    .ThenInclude(cs => cs.Class)
                .Include(eds => eds.ClassSections)
                    .ThenInclude(cs => cs.Section)
                .Where(eds => eds.ExamDate >= startDate && eds.ExamDate <= endDate);
            
            if (campusId.HasValue && campusId > 0)
                examDatesQuery = examDatesQuery.Where(eds => eds.CampusId == campusId);
            
            var examDateSheets = await examDatesQuery.ToListAsync();
            
            var examDates = examDateSheets.Select(eds => new
                {
                    id = eds.Id,
                    date = eds.ExamDate.ToString("yyyy-MM-dd"),
                    title = eds.Exam.Name + " - " + eds.Subject.Name,
                    description = "Exam on " + eds.ExamDate.ToString("MMM dd, yyyy"),
                    classSection = eds.ClassSections.Any() 
                        ? string.Join(", ", eds.ClassSections
                            .Where(cs => cs.IsActive && cs.Class != null && cs.Section != null)
                            .Select(cs => $"{cs.Class.Name} - {cs.Section.Name}")) 
                        : "",
                    isHoliday = false,
                    type = "exam"
                })
                .ToList();
            
            var allEvents = calendarEvents.Concat(examDates).ToList();
            
            return Json(allEvents);
        }
        
        [HttpPost]
        public async Task<IActionResult> AddCalendarEvent([FromBody] CalendarEventDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (string.IsNullOrEmpty(dto.EventName) || !dto.CampusId.HasValue)
            {
                return BadRequest(new { success = false, message = "Event name and campus are required" });
            }
            
            var calendarEvent = new CalendarEvent
            {
                CampusId = dto.CampusId.Value,
                EventName = dto.EventName,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsHoliday = dto.IsHoliday,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUser?.UserName
            };
            
            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Calendar event added successfully", eventId = calendarEvent.Id });
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdateCalendarEvent([FromBody] CalendarEventUpdateDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (string.IsNullOrEmpty(dto.EventName) || dto.Id <= 0)
            {
                return BadRequest(new { success = false, message = "Event name and ID are required" });
            }
            
            var calendarEvent = await _context.CalendarEvents.FindAsync(dto.Id);
            if (calendarEvent == null)
            {
                return NotFound(new { success = false, message = "Event not found" });
            }
            
            calendarEvent.EventName = dto.EventName;
            calendarEvent.Description = dto.Description;
            calendarEvent.StartDate = dto.StartDate;
            calendarEvent.EndDate = dto.EndDate;
            calendarEvent.IsHoliday = dto.IsHoliday;
            calendarEvent.UpdatedAt = DateTime.Now;
            calendarEvent.UpdatedBy = currentUser?.UserName;
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Calendar event updated successfully" });
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteCalendarEvent(int id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null)
            {
                return NotFound(new { success = false, message = "Event not found" });
            }
            
            calendarEvent.IsActive = false;
            calendarEvent.UpdatedAt = DateTime.Now;
            var currentUser = await _userManager.GetUserAsync(User);
            calendarEvent.UpdatedBy = currentUser?.UserName;
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Calendar event deleted successfully" });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetNamazAttendanceTrends(int days = 7)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days);
            
            var attendanceData = new List<object>();
            
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var namazQuery = _context.NamazAttendance
                    .AsNoTracking()
                    .Where(na => na.Date == date);
                
                if (campusId.HasValue && campusId > 0)
                    namazQuery = namazQuery.Where(na => na.CampusId == campusId);
                
                var namazRecords = await namazQuery.ToListAsync();
                
                var studentsWithJamat = namazRecords.Count(r => r.StudentId.HasValue && r.Status == "WJ");
                var studentsTotal = namazRecords.Count(r => r.StudentId.HasValue);
                
                attendanceData.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    dayOfWeek = date.ToString("ddd"),
                    withJamat = studentsWithJamat,
                    qaza = namazRecords.Count(r => r.StudentId.HasValue && r.Status == "QZ"),
                    withoutJamat = namazRecords.Count(r => r.StudentId.HasValue && r.Status == "WOJ"),
                    total = studentsTotal,
                    percentage = studentsTotal > 0 ? Math.Round((double)studentsWithJamat / studentsTotal * 100, 1) : 0
                });
            }
            
            return Json(attendanceData);
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleToDo(int id)
        {
            var todo = await _context.ToDos.FindAsync(id);
            if (todo == null)
                return NotFound();
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (todo.UserId != currentUser?.Id)
                return Forbid();
            
            todo.IsCompleted = !todo.IsCompleted;
            todo.UpdatedAt = DateTime.Now;
            todo.UpdatedBy = currentUser?.UserName;
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, isCompleted = todo.IsCompleted });
        }
        
        [HttpPost]
        public async Task<IActionResult> AddToDo([FromBody] ToDoDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (string.IsNullOrEmpty(dto.Title))
            {
                return BadRequest(new { success = false, message = "Title is required" });
            }
            
            var todo = new ToDo
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority ?? "Medium",
                DueDate = dto.DueDate ?? DateTime.Today.AddDays(7),
                UserId = currentUser?.Id ?? "",
                CampusId = currentUser?.CampusId,
                IsCompleted = false,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUser?.UserName
            };
            
            _context.ToDos.Add(todo);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Task added successfully", todoId = todo.Id });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetToDoList()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var todos = await _context.ToDos
                .AsNoTracking()
                .Where(t => t.UserId == currentUser.Id && t.IsActive)
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    priority = t.Priority,
                    dueDate = t.DueDate,
                    isCompleted = t.IsCompleted
                })
                .ToListAsync();
            
            return Json(todos);
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteToDo(int id)
        {
            var todo = await _context.ToDos.FindAsync(id);
            if (todo == null)
                return NotFound(new { success = false, message = "Task not found" });
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (todo.UserId != currentUser?.Id)
                return Forbid();
            
            // Soft delete
            todo.IsActive = false;
            todo.UpdatedAt = DateTime.Now;
            todo.UpdatedBy = currentUser?.UserName;
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Task deleted successfully" });
        }
            [HttpGet]
        public async Task<IActionResult> GetEmployeeAttendanceTrends(int days = 7)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days);
            
            var attendanceData = new List<object>();
            
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var employeeQuery = _context.EmployeeAttendance
                    .Include(ea => ea.Employee) // Include Employee navigation property
                    .AsNoTracking()
                    .Where(ea => ea.Date == date);
                
                if (campusId.HasValue && campusId > 0)
                    employeeQuery = employeeQuery.Where(ea => ea.Employee.CampusId == campusId);
                
                var employeeRecords = await employeeQuery.ToListAsync();
                
                var present = employeeRecords.Count(r => r.Status == "P");
                var absent = employeeRecords.Count(r => r.Status == "A");
                var late = employeeRecords.Count(r => r.Status == "T" || r.Status == "S");
                var total = present + absent + late;
                
                attendanceData.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    dayOfWeek = date.ToString("ddd"),
                    present = present,
                    absent = absent,
                    late = late,
                    total = total,
                    percentage = total > 0 ? Math.Round((double)present / total * 100, 1) : 0
                });
            }
            
            return Json(attendanceData);
        }
    }
    
    public class CalendarEventDto
    {
        public int? CampusId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsHoliday { get; set; }
    }
    
    public class CalendarEventUpdateDto
    {
        public int Id { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsHoliday { get; set; }
    }
    
    public class ToDoDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
