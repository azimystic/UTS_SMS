using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
 using UTS_SMS.Models;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    public class PayrollReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Main Payroll Reports Index
        // GET: Payroll/Index
        public async Task<IActionResult> Index(int? forMonth, int? forYear,
            int campusFilter = 0, string roleFilter = "All", string statusFilter = "All", string sortBy = "Name")
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // default month = previous month
            if (!forMonth.HasValue) forMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            if (!forYear.HasValue) forYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            ViewBag.ForMonth = forMonth;
            ViewBag.ForYear = forYear;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortBy = sortBy;

            // Get filter data
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            ViewBag.Roles = await _context.Employees
                .Where(e => e.IsActive)
                .Select(e => e.Role)
                .Distinct()
                .ToListAsync();

            // Employees
            var employeesQuery = _context.Employees
                .Include(e => e.Campus)
                .Where(e => e.IsActive)
                .AsQueryable();

            if (campusFilter > 0)
                employeesQuery = employeesQuery.Where(e => e.CampusId == campusFilter);

            if (roleFilter != "All")
                employeesQuery = employeesQuery.Where(e => e.Role == roleFilter);

            var employees = await employeesQuery.ToListAsync();

            // Payroll masters for month-year
            var payrollsQuery = _context.PayrollMasters
                .Include(p => p.Employee)
                .Include(p => p.Campus)
                .Include(p => p.Transactions)
                .Where(p => p.ForMonth == forMonth && p.ForYear == forYear);

            var payrollMastersList = await payrollsQuery.ToListAsync();

            var payrollReports = new List<PayrollReportViewModel>();

            foreach (var emp in employees)
            {
                var payroll = payrollMastersList.FirstOrDefault(p => p.EmployeeId == emp.Id);

                decimal totalPayable;
                decimal totalPaid;
                decimal balance;
                string status;

                if (payroll != null)
                {
                    // When there's a payroll record, deduct attendance deduction from total payable
                    totalPayable = payroll.NetSalary - payroll.AttendanceDeduction;
                    totalPaid = payroll.AmountPaid;
                    balance = payroll.Balance;
                    status = GetPayrollStatus(payroll);
                }
                else
                {
                    // No payroll yet → use SalaryDefinition
                    var salary = await _context.SalaryDefinitions
                        .Where(s => s.EmployeeId == emp.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedDate)
                        .FirstOrDefaultAsync();

                    totalPayable = salary?.NetSalary ?? 0;
                    totalPaid = 0;
                    balance = totalPayable;
                    status = "NotPaid";
                }

                payrollReports.Add(new PayrollReportViewModel
                {
                    Employee = emp,
                    Campus = emp.Campus,
                    PayrollMaster = payroll,
                    TotalPayable = totalPayable,
                    TotalPaid = totalPaid,
                    Balance = balance,
                    Status = status
                });
            }

            // Filter by status
            if (statusFilter != "All")
                payrollReports = payrollReports.Where(pr => pr.Status == statusFilter).ToList();

            // Sorting
            payrollReports = sortBy switch
            {
                "Name" => payrollReports.OrderBy(pr => pr.Employee.FullName).ToList(),
                "Role" => payrollReports.OrderBy(pr => pr.Employee.Role).ToList(),
                "Campus" => payrollReports.OrderBy(pr => pr.Campus.Name).ToList(),
                "Status" => payrollReports.OrderBy(pr => pr.Status).ToList(),
                "Balance" => payrollReports.OrderByDescending(pr => pr.Balance).ToList(),
                "Salary" => payrollReports.OrderByDescending(pr => pr.TotalPayable).ToList(),
                _ => payrollReports.OrderBy(pr => pr.Employee.FullName).ToList()
            };

            return View(payrollReports);
        }

        // Payroll Details (by month-year)
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Campus)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null) return NotFound();

            // Fetch all payrolls for employee
            // Fetch all payrolls for employee
            var payrollRecords = await _context.PayrollMasters
                .Include(p => p.Transactions)
                .Where(p => p.EmployeeId == id)
                .OrderBy(p => p.ForYear).ThenBy(p => p.ForMonth)  // 👈 ascending order (oldest first)
                .ToListAsync();


            var latestPayroll = payrollRecords
                .OrderByDescending(p => p.ForYear)
                .ThenByDescending(p => p.ForMonth)
                .FirstOrDefault();

            var viewModel = new PayrollDetailReportViewModel
            {
                Employee = employee,
                PayrollRecords = payrollRecords,
                TotalPayable = payrollRecords.Sum(p => p.NetSalary - p.PreviousBalance),


                TotalPaid = payrollRecords.Sum(p => p.AmountPaid),
                Balance = latestPayroll?.Balance ?? 0,
                LatestPayroll = latestPayroll
            };

            return View(viewModel);
        }


        // Transaction receipt (month-year not needed, just from PayrollMaster)
        public async Task<IActionResult> TransactionReceipt(int id)
        {
            var master = await _context.PayrollMasters
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Campus)
                .Include(p => p.Transactions)
                    .ThenInclude(t => t.Account)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (master == null) return NotFound();

            var viewModel = new PayrollReceiptViewModel
            {
                Master = master,
                Transactions = master.Transactions
                    .OrderByDescending(t => t.PaymentDate) // latest first
                    .ToList()
            };

            // send to view
            return View(viewModel);
        }




        // GET: Payroll/PrintReports
        public async Task<IActionResult> PrintReports(
            int? forMonth,
            int? forYear,
            int campusFilter = 0,
            string roleFilter = "All",
            string statusFilter = "All",
            string sortBy = "Name")
        {
            // 1️⃣ Default to previous month if not provided
            var defaultDate = DateTime.Now.AddMonths(-1);
            var selectedMonth = forMonth ?? defaultDate.Month;
            var selectedYear = forYear ?? defaultDate.Year;

            // 2️⃣ Base employee query
            var query = _context.Employees
                .Include(e => e.Campus)
                .Where(e => e.IsActive);

            // 3️⃣ Apply campus filter
            if (campusFilter > 0)
                query = query.Where(e => e.CampusId == campusFilter);

            // 4️⃣ Apply role filter
            if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
                query = query.Where(e => e.Role == roleFilter);

            var employees = await query.ToListAsync();
            // Determine selected campus object
            if (campusFilter > 0)
            {
                ViewBag.Campus = await _context.Campuses.FirstOrDefaultAsync(c => c.Id == campusFilter);
            }
            else
            {
                ViewBag.Campus = await _context.Campuses.FirstOrDefaultAsync(); // Default campus
            }
            // 5️⃣ Prepare report list
            var reportData = new List<PayrollReportViewModel>();

            foreach (var emp in employees)
            {
                // Fetch PayrollMaster for selected month/year
                var payroll = await _context.PayrollMasters
                    .Include(p => p.Transactions)
                    .FirstOrDefaultAsync(p => p.EmployeeId == emp.Id &&
                                              p.ForMonth == selectedMonth &&
                                              p.ForYear == selectedYear);

                decimal totalPayable;
                decimal totalPaid;
                decimal balance;
                string status;

                if (payroll != null)
                {
                    // When there's a payroll record, deduct attendance deduction from total payable
                    totalPayable = payroll.BasicSalary + payroll.Allowances + payroll.Bonus + payroll.PreviousBalance - payroll.Deductions - payroll.AttendanceDeduction;
                    totalPaid = payroll.Transactions?.Sum(t => t.AmountPaid) ?? 0;
                    balance = totalPayable - totalPaid;
                    status = balance == 0 ? "Paid" : balance > 0 ? "Partial" : "Advance";
                }
                else
                {
                    // Employee without PayrollMaster
                    var salaryDef = await _context.SalaryDefinitions
                        .Where(s => s.EmployeeId == emp.Id && s.IsActive)
                        .OrderByDescending(s => s.CreatedDate)
                        .FirstOrDefaultAsync();

                    totalPayable = salaryDef?.NetSalary ?? 0;
                    totalPaid = 0;
                    balance = totalPayable; // Full due
                    status = "NotPaid";
                }

                reportData.Add(new PayrollReportViewModel
                {
                    Employee = emp,
                    PayrollMaster = payroll,
                    Campus = emp.Campus,
                    TotalPayable = totalPayable,
                    TotalPaid = totalPaid,
                    Balance = balance,
                    Status = status
                });
            }

            // 6️⃣ Apply status filter
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                reportData = statusFilter switch
                {
                    "Paid" => reportData.Where(r => r.Balance == 0).ToList(),
                    "Partial" => reportData.Where(r => r.Balance > 0).ToList(),
                    "NotPaid" => reportData.Where(r => r.Status == "NotPaid").ToList(),
                    _ => reportData
                };
            }

            // 7️⃣ Sorting
            reportData = sortBy switch
            {
                "Role" => reportData.OrderBy(r => r.Employee.Role).ToList(),
                "Campus" => reportData.OrderBy(r => r.Campus.Name).ToList(),
                "Status" => reportData.OrderBy(r => r.Balance).ToList(),
                "Balance" => reportData.OrderByDescending(r => r.Balance).ToList(),
                _ => reportData.OrderBy(r => r.Employee.FullName).ToList()
            };

            // 8️⃣ Pass filter info to view
            ViewBag.ForMonth = selectedMonth;
            ViewBag.ForYear = selectedYear;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortBy = sortBy;
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            ViewBag.Roles = await _context.Employees.Select(e => e.Role).Distinct().ToListAsync();

            return View(reportData);
        }


















        // Payroll Transactions Report
        public async Task<IActionResult> Transactions(DateTime? startDate, DateTime? endDate,
            int campusFilter = 0, string roleFilter = "All", string sortBy = "Date")
        {
            if (!startDate.HasValue) startDate = DateTime.Now.Date;
            if (!endDate.HasValue) endDate = DateTime.Now.Date;

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.SortBy = sortBy;

            // Get filter data
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            ViewBag.Roles = await _context.Employees
                .Where(e => e.IsActive)
                .Select(e => e.Role)
                .Distinct()
                .ToListAsync();

            var transactionsQuery = _context.PayrollTransactions
                .Include(t => t.PayrollMaster)
                    .ThenInclude(p => p.Employee)
                .Include(t => t.Campus)
                .Include(t => t.Account)
                .Where(t => t.PaymentDate.Date >= startDate.Value.Date &&
                           t.PaymentDate.Date <= endDate.Value.Date);

            // Apply filters
            if (campusFilter > 0)
                transactionsQuery = transactionsQuery.Where(t => t.CampusId == campusFilter);

            if (roleFilter != "All")
                transactionsQuery = transactionsQuery.Where(t => t.PayrollMaster.Employee.Role == roleFilter);

            var transactions = await transactionsQuery.ToListAsync();

            // Apply sorting
            transactions = sortBy switch
            {
                "Date" => transactions.OrderByDescending(t => t.PaymentDate).ToList(),
                "Employee" => transactions.OrderBy(t => t.PayrollMaster.Employee.FullName).ToList(),
                "Amount" => transactions.OrderByDescending(t => t.AmountPaid).ToList(),
                "Role" => transactions.OrderBy(t => t.PayrollMaster.Employee.Role).ToList(),
                "Campus" => transactions.OrderBy(t => t.Campus.Name).ToList(),
                _ => transactions.OrderByDescending(t => t.PaymentDate).ToList()
            };

            return View(transactions);
        }

        // Helper method to determine payroll status
        private string GetPayrollStatus(PayrollMaster payroll)
        {
            if (payroll.AmountPaid == 0)
                return "NotPaid";
            else if (payroll.AmountPaid >= payroll.NetSalary)
                return "Paid";
            else
                return "Partial";
        }
    }
}
