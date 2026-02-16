using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.ViewModels;

namespace SMS.Controllers
{
    public class TransactionReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TransactionReports
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate,
            int? classFilter, int? sectionFilter, string sortBy = "Date", decimal? minAmount = null, decimal? maxAmount = null)
        {
            // Set default dates if not provided
            if (!startDate.HasValue) startDate = DateTime.Today;
            if (!endDate.HasValue) endDate = DateTime.Today;

            var query = _context.BillingTransactions
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.SectionObj)
                .Include(t => t.Campus)
                .Include(t => t.Account)
                .Where(t => t.PaymentDate.Date >= startDate.Value.Date &&
                           t.PaymentDate.Date <= endDate.Value.Date &&
                           !t.ReceivedBy.StartsWith("System-SalaryDeduction"));

            // Apply class filter
            if (classFilter.HasValue && classFilter.Value > 0)
            {
                query = query.Where(t => t.BillingMaster.Student.Class == classFilter.Value);
            }

            // Apply section filter
            if (sectionFilter.HasValue && sectionFilter.Value > 0)
            {
                query = query.Where(t => t.BillingMaster.Student.Section == sectionFilter.Value);
            }

            // Apply amount filters
            if (minAmount.HasValue)
            {
                query = query.Where(t => t.AmountPaid >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(t => t.AmountPaid <= maxAmount.Value);
            }

            // Apply sorting
            query = sortBy switch
            {
                "Student" => query.OrderBy(t => t.BillingMaster.Student.StudentName),
                "Class" => query.OrderBy(t => t.BillingMaster.Student.ClassObj.Name)
                              .ThenBy(t => t.BillingMaster.Student.SectionObj.Name)
                              .ThenBy(t => t.BillingMaster.Student.StudentName),
                "Section" => query.OrderBy(t => t.BillingMaster.Student.SectionObj.Name)
                                .ThenBy(t => t.BillingMaster.Student.StudentName),
                "Amount" => query.OrderByDescending(t => t.AmountPaid),
                _ => query.OrderByDescending(t => t.PaymentDate)
            };

            var transactions = await query.ToListAsync();

            ViewBag.Classes = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Sections = await _context.ClassSections.OrderBy(s => s.Name).ToListAsync();
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.ClassFilter = classFilter;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.SortBy = sortBy;
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;

            var viewModel = new TransactionReportViewModel
            {
                Transactions = transactions,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            return View(viewModel);
        }

        // GET: TransactionReports/PrintReport
        public async Task<IActionResult> PrintReport(DateTime? startDate, DateTime? endDate,
            int? classFilter, int? sectionFilter, string sortBy = "Date")
        {
            if (!startDate.HasValue) startDate = DateTime.Today;
            if (!endDate.HasValue) endDate = DateTime.Today;

            var query = _context.BillingTransactions
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.SectionObj)
                .Include(t => t.Campus)
                .Where(t => t.PaymentDate.Date >= startDate.Value.Date &&
                           t.PaymentDate.Date <= endDate.Value.Date &&
                           !t.ReceivedBy.StartsWith("System-SalaryDeduction"));

            if (classFilter.HasValue && classFilter.Value > 0)
            {
                query = query.Where(t => t.BillingMaster.Student.Class == classFilter.Value);
            }

            if (sectionFilter.HasValue && sectionFilter.Value > 0)
            {
                query = query.Where(t => t.BillingMaster.Student.Section == sectionFilter.Value);
            }

            query = sortBy switch
            {
                "Student" => query.OrderBy(t => t.BillingMaster.Student.StudentName),
                "Class" => query.OrderBy(t => t.BillingMaster.Student.ClassObj.Name)
                              .ThenBy(t => t.BillingMaster.Student.SectionObj.Name),
                "Section" => query.OrderBy(t => t.BillingMaster.Student.SectionObj.Name),
                "Amount" => query.OrderByDescending(t => t.AmountPaid),
                _ => query.OrderByDescending(t => t.PaymentDate)
            };

            var transactions = await query.ToListAsync();
            ViewBag.Campus = await _context.Campuses.FirstOrDefaultAsync();

            var viewModel = new TransactionReportViewModel
            {
                Transactions = transactions,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            return View(viewModel);
        }
    }
}