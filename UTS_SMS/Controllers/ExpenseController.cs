using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExpenseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpenseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Expense
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            // Default to current month/year if not specified
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            IQueryable<Expense> expensesQuery = _context.Expenses
                .Include(e => e.Account)
                .Include(e => e.Campus)
                .Where(e => e.IsActive && e.Month == currentMonth && e.Year == currentYear);

            if (campusId.HasValue && campusId > 0)
            {
                expensesQuery = expensesQuery.Where(e => e.CampusId == campusId);
            }

            var expenses = await expensesQuery.OrderByDescending(e => e.CreatedDate).ToListAsync();

            ViewBag.Campuses = campusId.HasValue && campusId > 0 
                ? await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync()
                : await _context.Campuses.Where(c => c.IsActive).ToListAsync();

            ViewBag.CurrentMonth = currentMonth;
            ViewBag.CurrentYear = currentYear;
            ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);

            // Generate month/year options for filter
            ViewBag.MonthOptions = Enumerable.Range(1, 12).Select(m => new { 
                Value = m, 
                Text = new DateTime(2000, m, 1).ToString("MMMM") 
            });

            var currentYearVal = DateTime.Now.Year;
            ViewBag.YearOptions = Enumerable.Range(currentYearVal - 2, 5).Select(y => new { 
                Value = y, 
                Text = y.ToString() 
            });

            return View(expenses);
        }

        // GET: Expense/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var expense = await _context.Expenses
                .Include(e => e.Account)
                .Include(e => e.Campus)
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (expense == null)
                return NotFound();

            return View(expense);
        }

        // GET: Expense/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name");
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            }

            // Set default month and year
            ViewBag.DefaultMonth = DateTime.Now.Month;
            ViewBag.DefaultYear = DateTime.Now.Year;

            return View();
        }

        // POST: Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Amount,ExpenseDate,Month,Year,AccountId,Category,Reference,CampusId")] Expense expense)
        {
            ModelState.Remove("Account");
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                expense.CreatedDate = DateTime.Now;
                expense.CreatedBy = User.Identity?.Name;

                // Set month and year from expense date if not provided
                if (expense.Month == 0)
                    expense.Month = expense.ExpenseDate.Month;
                if (expense.Year == 0)
                    expense.Year = expense.ExpenseDate.Year;

                _context.Add(expense);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Expense added successfully!";
                return RedirectToAction(nameof(Index), new { month = expense.Month, year = expense.Year });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle", expense.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", expense.CampusId);
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle", expense.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", expense.CampusId);
            }

            return View(expense);
        }

        // GET: Expense/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null || !expense.IsActive)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle", expense.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", expense.CampusId);
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle", expense.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", expense.CampusId);
            }

            return View(expense);
        }

        // POST: Expense/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Amount,ExpenseDate,Month,Year,AccountId,Category,Reference,CampusId")] Expense expense)
        {
            if (id != expense.Id)
                return NotFound();

            ModelState.Remove("Account");
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingExpense = await _context.Expenses.FindAsync(id);
                    if (existingExpense == null)
                        return NotFound();

                    existingExpense.Name = expense.Name;
                    existingExpense.Description = expense.Description;
                    existingExpense.Amount = expense.Amount;
                    existingExpense.ExpenseDate = expense.ExpenseDate;
                    existingExpense.Month = expense.Month != 0 ? expense.Month : expense.ExpenseDate.Month;
                    existingExpense.Year = expense.Year != 0 ? expense.Year : expense.ExpenseDate.Year;
                    existingExpense.AccountId = expense.AccountId;
                    existingExpense.Category = expense.Category;
                    existingExpense.Reference = expense.Reference;
                    existingExpense.ModifiedDate = DateTime.Now;
                    existingExpense.ModifiedBy = User.Identity?.Name;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expense.Id))
                        return NotFound();
                    else
                        throw;
                }

                TempData["SuccessMessage"] = "Expense updated successfully!";
                return RedirectToAction(nameof(Index), new { month = expense.Month, year = expense.Year });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle", expense.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", expense.CampusId);
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle", expense.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", expense.CampusId);
            }

            return View(expense);
        }

        // POST: Expense/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                expense.IsActive = false;
                expense.ModifiedDate = DateTime.Now;
                expense.ModifiedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Expense deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.Id == id);
        }
    }
}