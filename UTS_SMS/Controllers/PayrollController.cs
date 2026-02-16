using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.ViewModels;
using EmployeeAttendanceViewModel = UTS_SMS.ViewModels.EmployeeAttendanceViewModel;

namespace UTS_SMS.Controllers
{
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PayrollController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payroll
        public async Task<IActionResult> Index(string searchString, string roleFilter, int? forMonth, int? forYear)
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            ViewData["CurrentFilter"] = searchString;
            ViewData["RoleFilter"] = roleFilter;

            // Dropdown values - filtered by campus
            var rolesQuery = _context.Employees.Where(e => e.IsActive);
            if (userCampusId.HasValue && userCampusId.Value != 0)
            {
                rolesQuery = rolesQuery.Where(e => e.CampusId == userCampusId.Value);
            }
            ViewBag.Roles = await rolesQuery
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            // Get all years from current year back to earliest payroll or 5 years, whichever is earlier
            var earliestPayrollYear = await _context.PayrollMasters
                .Select(p => p.ForYear)
                .OrderBy(y => y)
                .FirstOrDefaultAsync();
            
            var currentYear = DateTime.Now.Year;
            var startYear = earliestPayrollYear > 0 ? Math.Min(earliestPayrollYear, currentYear - 5) : currentYear - 5;
            
            ViewBag.AvailableYears = Enumerable.Range(startYear, currentYear - startYear + 1)
                .OrderByDescending(y => y)
                .ToList();

            var defaultDate = DateTime.Now.AddMonths(-1);
            var selectedMonth = forMonth ?? defaultDate.Month;
            var selectedYear = forYear ?? defaultDate.Year;

            ViewData["ForMonth"] = selectedMonth;
            ViewData["ForYear"] = selectedYear;

            var now = DateTime.Now;
            int currentMonth = now.Month;
             currentYear = now.Year;

            // Pre-fetch salary deductions for all employees for the selected month/year
            var salaryDeductionsByEmployee = await _context.SalaryDeductions
                .Where(sd => sd.ForMonth == selectedMonth && sd.ForYear == selectedYear)
                .GroupBy(sd => sd.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    TotalDeductions = g.Sum(sd => sd.AmountDeducted)
                })
                .ToDictionaryAsync(x => x.EmployeeId, x => x.TotalDeductions);

            // Calculate the start and end date of the selected month
            var selectedMonthStart = new DateTime(selectedYear, selectedMonth, 1);
            var selectedMonthEnd = selectedMonthStart.AddMonths(1).AddDays(-1);

            var employeesQuery = _context.Employees
                .Include(e => e.Campus)
                .Where(e => e.IsActive && 
                           e.JoiningDate <= selectedMonthEnd); // Only show employees whose joining date is before or within the selected month

            // Filter by campus for non-owner users
            if (userCampusId.HasValue && userCampusId.Value != 0)
            {
                employeesQuery = employeesQuery.Where(e => e.CampusId == userCampusId.Value);
            }

            var employeesWithPayroll = employeesQuery
                .Select(e => new
                {
                    Employee = e,
                    SelectedPayroll = _context.PayrollMasters
                        .Where(p => p.EmployeeId == e.Id &&
                                    p.ForMonth == selectedMonth &&
                                    p.ForYear == selectedYear)
                        .FirstOrDefault(),

                    TeacherAssignmentsCount = e.Role == "Teacher" ?
                        _context.TeacherAssignments
                            .Count(ta => ta.TeacherId == e.Id && ta.IsActive) : 0,

                    HasFuturePayroll = _context.PayrollMasters.Any(p =>
                        p.EmployeeId == e.Id &&
                        (p.ForYear > selectedYear ||
                         (p.ForYear == selectedYear && p.ForMonth > selectedMonth)))
                });

            var employeesList = await employeesWithPayroll.ToListAsync();

            var viewModelList = employeesList.Select(x => new EmployeePayrollViewModel
            {
                Employee = x.Employee,
                PayrollMaster = x.SelectedPayroll,
                TeacherAssignmentsCount = x.TeacherAssignmentsCount,
                RowColor = x.SelectedPayroll == null ? "bg-red-100" :
                           x.SelectedPayroll.Balance == 0 ? "bg-green-100" :
                           x.SelectedPayroll.Balance > 0 ? "bg-yellow-100" : "bg-blue-100",

                // ✅ CanProcess logic
                CanProcess =
                    !x.HasFuturePayroll && // no future payroll
                    (
                        // Case 1: Selected year == current year
                        (selectedYear == currentYear &&
                         selectedMonth == currentMonth - 1 &&
                         (x.SelectedPayroll == null || x.SelectedPayroll.Balance > 0))

                        ||

                        // Case 2: Selected year < current year → still allow last month of previous year
                        (selectedYear < currentYear &&
                         selectedMonth == 12 &&
                         (x.SelectedPayroll == null || x.SelectedPayroll.Balance != 0))
                    ),

                // Get salary deductions from pre-fetched dictionary
                SalaryDeductionsTotal = salaryDeductionsByEmployee.GetValueOrDefault(x.Employee.Id, 0)
            }).ToList();


            return View(viewModelList);
        }
        [HttpGet]
        public async Task<IActionResult> Create(int id, int? forMonth, int? forYear)
        {
            // Load employee with current salary definition
            var employee = await _context.Employees
                .Include(e => e.Campus)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null) return NotFound();

            // Validate month and year - only allow past months
            var selectedMonth = forMonth ?? DateTime.Now.AddMonths(-1).Month;
            var selectedYear = forYear ?? DateTime.Now.Year;

            // If selected month is current or future, go to previous month
            if ((selectedYear > DateTime.Now.Year) ||
                (selectedYear == DateTime.Now.Year && selectedMonth >= DateTime.Now.Month))
            {
                selectedMonth = DateTime.Now.AddMonths(-1).Month;
                selectedYear = DateTime.Now.AddMonths(-1).Year;
            }

            // Get current salary definition
            var salaryDefinition = await _context.SalaryDefinitions
                .FirstOrDefaultAsync(sd => sd.EmployeeId == id && sd.IsActive);

            if (salaryDefinition == null)
                return BadRequest("No active salary definition found for this employee.");

            // Check for existing payroll
            var existingPayroll = await _context.PayrollMasters
                .Where(p => p.EmployeeId == id &&
                           p.ForMonth == selectedMonth &&
                           p.ForYear == selectedYear)
                .FirstOrDefaultAsync();

            if (existingPayroll != null)
            {
                if (existingPayroll.Balance == 0)
                {
                    TempData["ErrorMessage"] = $"Payroll for {selectedMonth}/{selectedYear} is already fully paid.";
                    return RedirectToAction("Index");
                }
                // If payroll exists but has balance, allow editing
            }

            // Get attendance records for the month
            var startDate = new DateTime(selectedYear, selectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var attendanceRecords = await _context.EmployeeAttendance
                .Where(a => a.EmployeeId == id &&
                           a.Date >= startDate &&
                           a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();

            // Get holidays for the month from CalendarEvents
            var monthHolidays = await _context.CalendarEvents
                .Where(ce => ce.CampusId == employee.CampusId &&
                            ce.IsHoliday &&
                            ce.IsActive &&
                            ce.StartDate >= startDate &&
                            ce.StartDate <= endDate)
                .OrderBy(ce => ce.StartDate)
                .ToListAsync();

            // Get holidays for the month from AcademicCalendar (for attendance deduction calculation)
            var holidays = await _context.AcademicCalendars
                .Where(ac => ac.CampusId == employee.CampusId &&
                            ac.Date >= startDate &&
                            ac.Date <= endDate &&
                            ac.IsActive)
                .Select(ac => ac.Date.Date)
                .ToListAsync();

            // Calculate attendance deductions
            decimal dailySalary = salaryDefinition.BasicSalary / DateTime.DaysInMonth(selectedYear, selectedMonth);
            decimal attendanceDeduction = 0;
            var attendanceViewModels = new List<EmployeeAttendanceViewModel>();

            // Create records for all days in the month
            for (int day = 1; day <= DateTime.DaysInMonth(selectedYear, selectedMonth); day++)
            {
                var currentDate = new DateTime(selectedYear, selectedMonth, day);
                var attendance = attendanceRecords.FirstOrDefault(a => a.Date.Date == currentDate.Date);
                var isHoliday = holidays.Contains(currentDate.Date);

                var deduction = 0m;
                var status = "A"; // Default to Absent

                if (isHoliday)
                {
                    // If it's a holiday, no deduction regardless of attendance
                    status = "H"; // Holiday
                    deduction = 0;
                }
                else if (attendance != null)
                {
                    status = attendance.Status;
                    if (attendance.Status == "A") // Absent
                    {
                        deduction = dailySalary;
                    }
                    else if (attendance.Status == "S") // Short Leave
                    {
                        deduction = dailySalary * 0.5m; // 50% deduction for short leave
                    }
                }
                else
                {
                    // No record found, consider as absent (only if not a holiday)
                    deduction = dailySalary;
                    status = "A";
                }

                attendanceDeduction += deduction;

                attendanceViewModels.Add(new EmployeeAttendanceViewModel
                {
                    Date = currentDate,
                    TimeIn = attendance?.TimeIn,
                    TimeOut = attendance?.TimeOut,
                    Status = status,
                    DailySalary = dailySalary,
                    DeductionAmount = deduction,
                    Remarks = attendance?.Remarks
                });
            }

            // Get previous balance
            decimal previousBalance = 0;
            var lastPayroll = await _context.PayrollMasters
      .Where(p => p.EmployeeId == id &&
          (p.BasicSalary + p.Allowances
           - p.Deductions - p.AttendanceDeduction + p.Bonus +
           + p.PreviousBalance - p.AmountPaid) != 0)
      .OrderByDescending(p => p.ForYear)
      .ThenByDescending(p => p.ForMonth)
      .FirstOrDefaultAsync();


            if (lastPayroll != null)
            {
                previousBalance = lastPayroll.Balance;
            }

            // Get student fee deductions if employee is a parent
            var studentFeeDeductions = new List<StudentFeeDeductionDetail>();
            decimal studentFeeDeductionTotal = 0;
            bool isEmployeeParent = false;

            // Check if employee is assigned as parent through StudentCategoryAssignment
            var studentCategoryAssignments = await _context.StudentCategoryAssignments
                .Include(sca => sca.Student)
                    .ThenInclude(s => s.ClassObj)
                .Where(sca => sca.EmployeeId == id && sca.IsActive)
                .ToListAsync();

            if (studentCategoryAssignments.Any())
            {
                isEmployeeParent = true;

                foreach (var assignment in studentCategoryAssignments)
                {
                    // Get the billing for this student for the selected month/year
                    var billing = await _context.BillingMaster
                        .Include(bm => bm.Transactions)
                        .FirstOrDefaultAsync(bm => 
                            bm.StudentId == assignment.StudentId && 
                            bm.ForMonth == selectedMonth && 
                            bm.ForYear == selectedYear);

                    if (billing != null)
                    {
                        // Calculate the deduction based on payment mode
                        // Normalize payment mode comparison to handle both variants
                        decimal amountToDeduct = 0;
                        var paymentMode = assignment.PaymentMode?.Replace(" ", "").ToLowerInvariant() ?? "";
                        if (paymentMode == "salarydeduction")
                        {
                            // Deduct the tuition fee (after discount) from salary
                            amountToDeduct = billing.TuitionFee;
                        }

                        if (amountToDeduct > 0)
                        {
                            studentFeeDeductions.Add(new StudentFeeDeductionDetail
                            {
                                StudentId = assignment.StudentId,
                                StudentName = assignment.Student?.StudentName ?? "Unknown",
                                ClassName = assignment.Student?.ClassObj?.Name ?? "N/A",
                                ForMonth = selectedMonth,
                                ForYear = selectedYear,
                                TuitionFee = billing.TuitionFee,
                                AmountDeducted = amountToDeduct,
                                PaymentMode = assignment.PaymentMode
                            });
                            studentFeeDeductionTotal += amountToDeduct;
                        }
                    }
                }
            }

            // Get attendance trends for the last 30 days for chart (include all statuses)
            var attendanceTrendStartDate = startDate.AddDays(-30);
            var attendanceTrends = new List<AttendanceChartData>();
            var allAttendanceRecords = await _context.EmployeeAttendance
                .Where(a => a.EmployeeId == id && a.Date >= attendanceTrendStartDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();

            for (var date = attendanceTrendStartDate; date <= endDate; date = date.AddDays(1))
            {
                var dayRecord = allAttendanceRecords.FirstOrDefault(a => a.Date.Date == date.Date);
                attendanceTrends.Add(new AttendanceChartData
                {
                    Date = date.ToString("MMM dd"),
                    DayOfWeek = date.ToString("ddd"),
                    Status = dayRecord?.Status ?? "N/A",
                    Present = dayRecord?.Status == "P" ? 1 : 0,
                    Absent = dayRecord?.Status == "A" ? 1 : 0,
                    Late = dayRecord?.Status == "T" ? 1 : 0,
                    ShortLeave = dayRecord?.Status == "S" ? 1 : 0,
                    Leave = dayRecord?.Status == "L" ? 1 : 0,
                    Percentage = dayRecord?.Status == "P" ? 100 : 0
                });
            }

            // Get multi-period attendance summaries (this month, last month, last year same month)
            var attendancePeriodSummaries = new List<AttendancePeriodSummary>();

            // This month
            var thisMonthRecords = allAttendanceRecords.Where(a => a.Date >= startDate && a.Date <= endDate).ToList();
            attendancePeriodSummaries.Add(new AttendancePeriodSummary
            {
                Period = "This Month",
                Present = thisMonthRecords.Count(a => a.Status == "P"),
                Absent = thisMonthRecords.Count(a => a.Status == "A"),
                Late = thisMonthRecords.Count(a => a.Status == "T"),
                ShortLeave = thisMonthRecords.Count(a => a.Status == "S"),
                Leave = thisMonthRecords.Count(a => a.Status == "L"),
                TotalDays = thisMonthRecords.Count
            });

            // Fetch attendance for last month and last year in a single query for optimization
            var lastMonthStart = startDate.AddMonths(-1);
            var lastMonthEnd = endDate.AddMonths(-1);
            var lastYearStart = new DateTime(selectedYear - 1, selectedMonth, 1);
            var lastYearEnd = lastYearStart.AddMonths(1).AddDays(-1);

            // Combined query for last month and last year attendance
            var historicalAttendance = await _context.EmployeeAttendance
                .AsNoTracking()
                .Where(a => a.EmployeeId == id &&
                    ((a.Date >= lastMonthStart && a.Date <= lastMonthEnd) ||
                     (a.Date >= lastYearStart && a.Date <= lastYearEnd)))
                .ToListAsync();

            var lastMonthRecords = historicalAttendance.Where(a => a.Date >= lastMonthStart && a.Date <= lastMonthEnd).ToList();
            attendancePeriodSummaries.Add(new AttendancePeriodSummary
            {
                Period = "Last Month",
                Present = lastMonthRecords.Count(a => a.Status == "P"),
                Absent = lastMonthRecords.Count(a => a.Status == "A"),
                Late = lastMonthRecords.Count(a => a.Status == "T"),
                ShortLeave = lastMonthRecords.Count(a => a.Status == "S"),
                Leave = lastMonthRecords.Count(a => a.Status == "L"),
                TotalDays = lastMonthRecords.Count
            });

            // Last year same month
            var lastYearRecords = historicalAttendance.Where(a => a.Date >= lastYearStart && a.Date <= lastYearEnd).ToList();
            attendancePeriodSummaries.Add(new AttendancePeriodSummary
            {
                Period = "Last Year",
                Present = lastYearRecords.Count(a => a.Status == "P"),
                Absent = lastYearRecords.Count(a => a.Status == "A"),
                Late = lastYearRecords.Count(a => a.Status == "T"),
                ShortLeave = lastYearRecords.Count(a => a.Status == "S"),
                Leave = lastYearRecords.Count(a => a.Status == "L"),
                TotalDays = lastYearRecords.Count
            });

            // Get salary deductions from database (read-only query with AsNoTracking)
            var salaryDeductionsList = await _context.SalaryDeductions
                .Include(sd => sd.Student)
                    .ThenInclude(s => s.ClassObj)
                .AsNoTracking()
                .Where(sd => sd.EmployeeId == id && sd.ForMonth == selectedMonth && sd.ForYear == selectedYear)
                .Select(sd => new SalaryDeductionItem
                {
                    Id = sd.Id,
                    StudentName = sd.Student.StudentName,
                    ClassName = sd.Student.ClassObj != null ? sd.Student.ClassObj.Name : "N/A",
                    AmountDeducted = sd.AmountDeducted,
                    DeductionDate = sd.DeductionDate
                })
                .ToListAsync();
            var totalSalaryDeductions = salaryDeductionsList.Sum(sd => sd.AmountDeducted);

            // Get payroll history - last 3 months only
            var payrollHistoryRecords = await _context.PayrollMasters
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.ForYear)
                .ThenByDescending(p => p.ForMonth)
                .Take(3)
                .ToListAsync();

            // Pre-fetch all next month payroll records to avoid N+1 queries
            var nextMonthKeys = payrollHistoryRecords.Select(p => new
            {
                Month = p.ForMonth == 12 ? 1 : p.ForMonth + 1,
                Year = p.ForMonth == 12 ? p.ForYear + 1 : p.ForYear
            }).ToList();

            // Step 1: Materialize all PayrollMasters for this employee to memory
            var allPayrollsForEmployee = await _context.PayrollMasters
                .Where(pm => pm.EmployeeId == id)
                .ToListAsync();

            // Step 2: Filter in memory to avoid LINQ translation error
            var nextMonthPayrolls = allPayrollsForEmployee
                .Where(pm => nextMonthKeys.Any(k => k.Month == pm.ForMonth && k.Year == k.Year))
                .ToDictionary(pm => (pm.ForMonth, pm.ForYear), pm => pm);

            var payrollHistory = new List<PayrollHistoryItem>();
            
            foreach (var p in payrollHistoryRecords)
            {
                // Check if this month's balance was transferred to the next month
                var nextMonth = p.ForMonth == 12 ? 1 : p.ForMonth + 1;
                var nextYear = p.ForMonth == 12 ? p.ForYear + 1 : p.ForYear;
                
                bool balanceTransferred = false;
                string transferredToMonthName = null;
                
                if (p.Balance != 0 && nextMonthPayrolls.TryGetValue((nextMonth, nextYear), out var nextMonthPayroll) && 
                    nextMonthPayroll.PreviousBalance == p.Balance)
                {
                    balanceTransferred = true;
                    transferredToMonthName = new DateTime(nextYear, nextMonth, 1).ToString("MMMM yyyy");
                }
                
                payrollHistory.Add(new PayrollHistoryItem
                {
                    Id = p.Id,
                    Month = p.ForMonth,
                    Year = p.ForYear,
                    MonthName = new DateTime(p.ForYear, p.ForMonth, 1).ToString("MMMM yyyy"),
                    BasicSalary = p.BasicSalary,
                    Allowances = p.Allowances,
                    Deductions = p.Deductions,
                    NetSalary = p.NetSalary,
                    AmountPaid = p.AmountPaid,
                    Balance = p.Balance,
                    Status = p.Balance == 0 ? "Paid" : p.Balance > 0 ? "Pending" : "Overpaid",
                    BalanceTransferredToNextMonth = balanceTransferred,
                    TransferredToMonthName = transferredToMonthName
                });
            }

            // Calculate attendance summary stats (separate Late and Short Leave)
            int totalPresent = attendanceViewModels.Count(a => a.Status == "P");
            int totalAbsent = attendanceViewModels.Count(a => a.Status == "A");
            int totalLate = attendanceViewModels.Count(a => a.Status == "T");
            int totalShortLeave = attendanceViewModels.Count(a => a.Status == "S");
            int totalLeave = attendanceViewModels.Count(a => a.Status == "L");
            int totalHolidays = attendanceViewModels.Count(a => a.Status == "H");
            int workingDays = attendanceViewModels.Count(a => a.Status != "H");
            double attendancePercentage = workingDays > 0 ? Math.Round((double)totalPresent / workingDays * 100, 1) : 0;
            
           
                var payrollViewModel = new PayrollCreateViewModel
            {
                EmployeeId = id,
                EmployeeName = employee.FullName,
                EmployeeRole = employee.Role,
                Employee = employee,
                EmployeeProfilePicture = employee.ProfilePicture,
                CampusName = employee.Campus?.Name ?? "N/A",
                ForMonth = selectedMonth,
                ForYear = selectedYear,
                BasicSalary = salaryDefinition.BasicSalary,
                Allowances = salaryDefinition.HouseRentAllowance + salaryDefinition.MedicalAllowance +
                            salaryDefinition.TransportationAllowance + salaryDefinition.OtherAllowances,
                Deductions = salaryDefinition.ProvidentFund + salaryDefinition.TaxDeduction +
                            salaryDefinition.OtherDeductions,
                AttendanceDeduction = attendanceDeduction,
                PreviousBalance = previousBalance,
                AttendanceRecords = attendanceViewModels,
                PaymentDate = DateTime.Now,
                // New properties
                MonthHolidays = monthHolidays,
                StudentFeeDeduction = studentFeeDeductionTotal,
                StudentFeeDeductions = studentFeeDeductions,
                IsEmployeeParent = isEmployeeParent,
                AttendanceTrends = attendanceTrends,
                AttendancePeriodSummaries = attendancePeriodSummaries,
                SalaryDeductions = salaryDeductionsList,
                TotalSalaryDeductions = totalSalaryDeductions,
                PayrollHistory = payrollHistory,
                TotalPresent = totalPresent,
                TotalAbsent = totalAbsent,
                TotalLate = totalLate,
                TotalShortLeave = totalShortLeave,
                TotalLeave = totalLeave,
                TotalHolidays = totalHolidays,
                AttendancePercentage = attendancePercentage
            };
            if (existingPayroll != null)
            {
                // For existing payroll with balance, show the remaining balance details
                // Keep all original calculation fields from database
                payrollViewModel.BasicSalary = existingPayroll.BasicSalary;
                payrollViewModel.Allowances = existingPayroll.Allowances;
                payrollViewModel.Deductions = existingPayroll.Deductions;
                payrollViewModel.AttendanceDeduction = existingPayroll.AttendanceDeduction;
                payrollViewModel.Bonus = existingPayroll.Bonus;
                payrollViewModel.PreviousBalance = existingPayroll.PreviousBalance;
                
                // Set CashPaid and OnlinePaid to 0 for new payment entry
                payrollViewModel.CashPaid = 0;
                payrollViewModel.OnlinePaid = 0;
                payrollViewModel.AmountPaid = 0; // This will be for new payment only
                
                // Set flags for existing record with balance
                payrollViewModel.IsExistingRecordWithBalance = existingPayroll.Balance != 0;
                payrollViewModel.ExistingBalance = existingPayroll.Balance;
                payrollViewModel.ExistingAmountPaid = existingPayroll.AmountPaid; // Track what was already paid
            }
            // For teachers, get subject performance data
            if (employee.Role == "Teacher")
            {
                payrollViewModel.TeacherPerformance = await GetTeacherPerformance(id, selectedMonth, selectedYear);
            }

            ViewData["AccountId"] = new SelectList(
                _context.BankAccounts
                    .Where(cs => cs.IsActive)
                    .Select(cs => new
                    {
                        cs.Id,
                        DisplayName = cs.BankName + " - " + cs.AccountTitle
                    })
                    .ToList(),
                "Id",
                "DisplayName"
            );

            return View(payrollViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PayrollCreateViewModel viewModel)
        {
            ModelState.Remove(nameof(PayrollCreateViewModel.EmployeeName));
            ModelState.Remove(nameof(PayrollCreateViewModel.EmployeeRole));
            ModelState.Remove(nameof(PayrollCreateViewModel.ForMonth));
            ModelState.Remove(nameof(PayrollCreateViewModel.ForYear));
            ModelState.Remove(nameof(PayrollCreateViewModel.AttendanceRecords));
            ModelState.Remove(nameof(PayrollCreateViewModel.TeacherPerformance));
            ModelState.Remove(nameof(PayrollCreateViewModel.Employee));
            ModelState.Remove(nameof(PayrollCreateViewModel.StudentFeeDeductions));
            ModelState.Remove(nameof(PayrollCreateViewModel.AttendanceTrends));
            ModelState.Remove(nameof(PayrollCreateViewModel.PayrollHistory));
            ModelState.Remove("OnlineAccount");
            ModelState.Remove("TransactionReference");
            ModelState.Remove("CampusName");
            ModelState.Remove("ReceivedBy");
            ModelState.Remove("EmployeeProfilePicture");
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == viewModel.EmployeeId);

            if (employee == null)
            {
                ModelState.AddModelError("", "Employee not found.");
                return View(viewModel);
            }

            // Prevent processing for current or future months
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            if ((viewModel.ForYear > currentYear) ||
                (viewModel.ForYear == currentYear && viewModel.ForMonth >= currentMonth))
            {
                ModelState.AddModelError("", "Cannot process payroll for current or future months.");
                viewModel.EmployeeName = employee.FullName;
                viewModel.EmployeeRole = employee.Role;
                ViewData["AccountId"] = new SelectList(
                    _context.BankAccounts
                        .Where(cs => cs.IsActive)
                        .Select(cs => new
                        {
                            cs.Id,
                            DisplayName = cs.BankName + " - " + cs.AccountTitle
                        })
                        .ToList(),
                    "Id",
                    "DisplayName"
                );
                return View(viewModel);
            }

            ModelState.Remove("ReceivedBy");
            if (ModelState.IsValid)
            {
                // Check for existing payroll first
                var existingMaster = await _context.PayrollMasters
                    .FirstOrDefaultAsync(p => p.EmployeeId == viewModel.EmployeeId &&
                                            p.ForMonth == viewModel.ForMonth &&
                                            p.ForYear == viewModel.ForYear);
                
                decimal totalPayable = 0;
                
                if (existingMaster != null)
                {
                    // For existing payroll, validate against remaining balance only
                    totalPayable = existingMaster.Balance; // Only the remaining balance
                    
                    if (viewModel.AmountPaid > totalPayable)
                    {
                        ModelState.AddModelError("AmountPaid", $"Amount paid (₨{viewModel.AmountPaid:N0}) cannot exceed remaining balance (₨{totalPayable:N0}).");
                        viewModel.EmployeeName = employee.FullName;
                        viewModel.EmployeeRole = employee.Role;
                        ViewData["AccountId"] = new SelectList(
                            _context.BankAccounts
                                .Where(cs => cs.IsActive)
                                .Select(cs => new
                                {
                                    cs.Id,
                                    DisplayName = cs.BankName + " - " + cs.AccountTitle
                                })
                                .ToList(),
                            "Id",
                            "DisplayName"
                        );
                        return View(viewModel);
                    }
                }
                else
                {
                    // For new payroll, calculate total payable from salary definition
                    var salaryDef = await _context.SalaryDefinitions
                        .FirstOrDefaultAsync(sd => sd.EmployeeId == viewModel.EmployeeId && sd.IsActive);
                    
                    if (salaryDef != null)
                    {
                        decimal basicSalary = salaryDef.BasicSalary;
                        decimal allowances = salaryDef.HouseRentAllowance + salaryDef.MedicalAllowance +
                                            salaryDef.TransportationAllowance + salaryDef.OtherAllowances;
                        decimal deductions = salaryDef.ProvidentFund + salaryDef.TaxDeduction + salaryDef.OtherDeductions;
                        totalPayable = basicSalary + allowances - deductions - viewModel.AttendanceDeduction + 
                                              viewModel.Bonus + viewModel.PreviousBalance;
                        
                        // Server-side validation: Paid Amount cannot exceed Payable Amount
                        if (viewModel.AmountPaid > totalPayable)
                        {
                            ModelState.AddModelError("AmountPaid", $"Amount paid (₨{viewModel.AmountPaid:N0}) cannot exceed total payable (₨{totalPayable:N0}).");
                            viewModel.EmployeeName = employee.FullName;
                            viewModel.EmployeeRole = employee.Role;
                            ViewData["AccountId"] = new SelectList(
                                _context.BankAccounts
                                    .Where(cs => cs.IsActive)
                                    .Select(cs => new
                                    {
                                        cs.Id,
                                        DisplayName = cs.BankName + " - " + cs.AccountTitle
                                    })
                                    .ToList(),
                                "Id",
                                "DisplayName"
                            );
                            return View(viewModel);
                        }
                    }
                }
                 

                // Check for existing payroll (already checked above, but keep for flow)

                PayrollMaster payrollMaster;

                if (existingMaster != null)
                {
                    // Update existing record - only add new values, don't replace
                    // Only update Bonus if a new bonus is provided (not zero)
                    if (viewModel.Bonus > 0)
                    {
                        existingMaster.Bonus += viewModel.Bonus;
                    }
                    
                    // Only update AttendanceDeduction if a new deduction is provided and different
                    if (viewModel.AttendanceDeduction > 0 && viewModel.AttendanceDeduction != existingMaster.AttendanceDeduction)
                    {
                        existingMaster.AttendanceDeduction = viewModel.AttendanceDeduction;
                    }
                    
                    // Add new payment to existing AmountPaid
                    existingMaster.AmountPaid += viewModel.AmountPaid;
                    
                    existingMaster.ModifiedDate = DateTime.Now;
                    existingMaster.ModifiedBy = User.Identity.Name;

                    _context.Update(existingMaster);
                    payrollMaster = existingMaster;
                }
                else
                {
                    // Get salary definition for calculations
                    var salaryDefinition = await _context.SalaryDefinitions
                        .FirstOrDefaultAsync(sd => sd.EmployeeId == viewModel.EmployeeId && sd.IsActive);

                    if (salaryDefinition == null)
                    {
                        ModelState.AddModelError("", "No active salary definition found.");
                        viewModel.EmployeeName = employee.FullName;
                        viewModel.EmployeeRole = employee.Role;
                        return View(viewModel);
                    }

                    // Create new record
                    payrollMaster = new PayrollMaster
                    {
                        EmployeeId = viewModel.EmployeeId,
                        ForMonth = viewModel.ForMonth,
                        ForYear = viewModel.ForYear,
                        BasicSalary = salaryDefinition.BasicSalary,
                        Allowances = salaryDefinition.HouseRentAllowance + salaryDefinition.MedicalAllowance +
                                    salaryDefinition.TransportationAllowance + salaryDefinition.OtherAllowances,
                        Deductions = salaryDefinition.ProvidentFund + salaryDefinition.TaxDeduction +
                                    salaryDefinition.OtherDeductions,
                        AttendanceDeduction = viewModel.AttendanceDeduction,
                        Bonus = viewModel.Bonus,
                         PreviousBalance = viewModel.PreviousBalance,
                        AmountPaid = viewModel.AmountPaid,
                        CreatedDate = DateTime.Now,
                        CreatedBy = User.Identity.Name,
                        CampusId = employee.CampusId
                    };

                    _context.Add(payrollMaster);
                }

                await _context.SaveChangesAsync(); // Save to get the ID for new records

                // Create payroll transaction only if payment was made
                if (viewModel.AmountPaid > 0)
                {
                    var payrollTransaction = new PayrollTransaction
                    {
                        PayrollMasterId = payrollMaster.Id,
                        AmountPaid = viewModel.AmountPaid,
                        CashPaid = viewModel.CashPaid,
                        OnlinePaid = viewModel.OnlinePaid,
                        OnlineAccount = viewModel.OnlineAccount,
                        TransactionReference = viewModel.TransactionReference,
                        PaymentDate = DateTime.Now,
                        ReceivedBy = User.Identity.Name,
                        CampusId = employee.CampusId
                    };
                    _context.Add(payrollTransaction);
                }

                await _context.SaveChangesAsync();

                var printUrl = Url.Action("TransactionReceipt", "PayrollReports", new { id = payrollMaster.Id });
                var indexUrl = Url.Action("Index", "Payroll");

                return Content($@"
    <script type='text/javascript'>
        var printWindow = window.open('{printUrl}', '_blank');
        printWindow.focus();
        window.location.href = '{indexUrl}';
    </script>", "text/html");


            }
            ViewData["AccountId"] = new SelectList(
                _context.BankAccounts
                    .Where(cs => cs.IsActive)
                    .Select(cs => new
                    {
                        cs.Id,
                        DisplayName = cs.BankName + " - " + cs.AccountTitle
                    })
                    .ToList(),
                "Id",
                "DisplayName"
            );
            // Reload employee details if validation fails
            viewModel.EmployeeName = employee.FullName;
            viewModel.EmployeeRole = employee.Role;
            return View(viewModel);
        }

        // Helper method to get teacher performance data
        private async Task<TeacherPerformanceViewModel> GetTeacherPerformance(int teacherId, int month, int year)
        {
            var performance = new TeacherPerformanceViewModel();

            // Get teacher's assigned subjects and classes
            var assignments = await _context.TeacherAssignments
                .Include(ta => ta.Subject)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .ToListAsync();

            performance.Assignments = assignments;

            // Get exam results for these subjects
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();
            var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
            var sectionIds = assignments.Select(a => a.SectionId).Distinct().ToList();

            var examResults = await _context.ExamMarks
                .Include(em => em.Exam)
                    .ThenInclude(e => e.ExamCategory)
                .Include(em => em.Student)
                .Include(em => em.Subject)
                .Include(em => em.Class)
                .Include(em => em.Section)
                .Where(em => subjectIds.Contains(em.SubjectId) &&
                            classIds.Contains(em.ClassId) &&
                            sectionIds.Contains(em.SectionId) &&
                            em.ExamDate >= startDate &&
                            em.ExamDate <= endDate)
                .ToListAsync();

            performance.ExamResults = examResults;

            // Calculate performance metrics
            if (examResults.Any())
            {
                performance.AveragePercentage = examResults.Average(er => er.Percentage);
                performance.PassPercentage = (decimal)examResults.Count(er => er.Status != "Fail") / examResults.Count * 100;
                performance.TotalStudents = examResults.Select(er => er.StudentId).Distinct().Count();
                performance.TotalExams = examResults.Select(er => er.ExamId).Distinct().Count();
            }

            // Get distinct exam categories
            performance.ExamCategories = examResults
                .Select(er => er.Exam?.ExamCategory?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            // Build class performance data (grouped by class and section)
            var classPerformances = new List<ClassPerformanceData>();
            
            foreach (var assignment in assignments)
            {
                var classSection = $"{assignment.Class?.Name}-{assignment.Section?.Name}";
                var existingClassPerf = classPerformances.FirstOrDefault(cp => 
                    cp.ClassName == assignment.Class?.Name && cp.SectionName == assignment.Section?.Name);

                if (existingClassPerf == null)
                {
                    existingClassPerf = new ClassPerformanceData
                    {
                        ClassName = assignment.Class?.Name ?? "Unknown",
                        SectionName = assignment.Section?.Name ?? "Unknown",
                        SubjectAverages = new List<SubjectAverageData>()
                    };
                    classPerformances.Add(existingClassPerf);
                }

                // Get exam results for this specific class, section, and subject
                var subjectResults = examResults.Where(er => 
                    er.ClassId == assignment.ClassId &&
                    er.SectionId == assignment.SectionId &&
                    er.SubjectId == assignment.SubjectId).ToList();

                if (subjectResults.Any())
                {
                    existingClassPerf.SubjectAverages.Add(new SubjectAverageData
                    {
                        SubjectName = assignment.Subject?.Name ?? "Unknown",
                        Average = subjectResults.Average(sr => sr.Percentage),
                        StudentCount = subjectResults.Select(sr => sr.StudentId).Distinct().Count()
                    });
                }
            }

            // Calculate class averages
            foreach (var classPerf in classPerformances)
            {
                if (classPerf.SubjectAverages.Any())
                {
                    classPerf.ClassAverage = classPerf.SubjectAverages.Average(sa => sa.Average);
                }
            }

            performance.ClassPerformances = classPerformances;

            // Build subject performance data (grouped by subject)
            var subjectPerformances = new List<SubjectPerformanceData>();
            
            foreach (var subjectId in subjectIds)
            {
                var subject = assignments.FirstOrDefault(a => a.SubjectId == subjectId)?.Subject;
                if (subject == null) continue;

                var subjectPerf = new SubjectPerformanceData
                {
                    SubjectName = subject.Name,
                    ClassAverages = new List<ClassAverageData>()
                };

                // Get all classes where this subject is taught
                var subjectAssignments = assignments.Where(a => a.SubjectId == subjectId).ToList();
                
                foreach (var assignment in subjectAssignments)
                {
                    var subjectClassResults = examResults.Where(er =>
                        er.SubjectId == subjectId &&
                        er.ClassId == assignment.ClassId &&
                        er.SectionId == assignment.SectionId).ToList();

                    if (subjectClassResults.Any())
                    {
                        subjectPerf.ClassAverages.Add(new ClassAverageData
                        {
                            ClassName = assignment.Class?.Name ?? "Unknown",
                            SectionName = assignment.Section?.Name ?? "Unknown",
                            Average = subjectClassResults.Average(sr => sr.Percentage),
                            StudentCount = subjectClassResults.Select(sr => sr.StudentId).Distinct().Count()
                        });
                    }
                }

                if (subjectPerf.ClassAverages.Any())
                {
                    subjectPerf.OverallAverage = subjectPerf.ClassAverages.Average(ca => ca.Average);
                    subjectPerformances.Add(subjectPerf);
                }
            }

            performance.SubjectPerformances = subjectPerformances;

            return performance;
        }

        // Action to view teacher performance details
        public async Task<IActionResult> TeacherPerformance(int id, int month, int year)
        {
            var performance = await GetTeacherPerformance(id, month, year);
            return View(performance);
        }

        // GET: Payroll/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payrollMaster = await _context.PayrollMasters
                .Include(p => p.Employee)
                .Include(p => p.Transactions)
                .Include(p => p.Campus)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payrollMaster == null)
            {
                return NotFound();
            }

            // Get attendance records for this payroll period
            var startDate = new DateTime(payrollMaster.ForYear, payrollMaster.ForMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var attendanceRecords = await _context.EmployeeAttendance
                .Where(a => a.EmployeeId == payrollMaster.EmployeeId &&
                           a.Date >= startDate &&
                           a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();

            var viewModel = new PayrollDetailViewModel
            {
                PayrollMaster = payrollMaster,
                AttendanceRecords = attendanceRecords
            };

            return View(viewModel);
        }
    }
}