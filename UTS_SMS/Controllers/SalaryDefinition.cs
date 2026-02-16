 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    public class SalaryDefinitionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryDefinitionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SalaryDefinition
        public async Task<IActionResult> Index(string searchString, string roleFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["RoleFilter"] = roleFilter;

            // Get roles for dropdown
            ViewBag.Roles = await _context.Employees
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            var salaryDefinitions = _context.SalaryDefinitions
                .Include(sd => sd.Employee)
                 .Where(sd => sd.IsActive)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                salaryDefinitions = salaryDefinitions.Where(sd =>
                    sd.Employee.FullName.Contains(searchString) ||
                    sd.Employee.CNIC.Contains(searchString) ||
                    sd.Employee.PhoneNumber.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                salaryDefinitions = salaryDefinitions.Where(sd => sd.Employee.Role == roleFilter);
            }

            // Order by employee name
            salaryDefinitions = salaryDefinitions.OrderBy(sd => sd.Employee.FullName);

            return View(await salaryDefinitions.ToListAsync());
        }

        // GET: SalaryDefinition/Create
        public async Task<IActionResult> Create()
        {
            // Get active employees who don't have an active salary definition
            var employeesWithSalary = await _context.SalaryDefinitions
                .Where(sd => sd.IsActive)
                .Select(sd => sd.EmployeeId)
                .ToListAsync();

            var availableEmployees = await _context.Employees
                .Where(e => e.IsActive && !employeesWithSalary.Contains(e.Id))
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewData["EmployeeId"] = new SelectList(availableEmployees, "Id", "FullName");

            return View();
        }

        // POST: SalaryDefinition/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalaryDefinition salaryDefinition)
        {
            ModelState.Remove("Employee");
            ModelState.Remove("CreatedBy");
 
            if (ModelState.IsValid)
            {
                // Check if employee already has an active salary definition
                var existingSalary = await _context.SalaryDefinitions
                    .FirstOrDefaultAsync(sd => sd.EmployeeId == salaryDefinition.EmployeeId && sd.IsActive);

                if (existingSalary != null)
                {
                    ModelState.AddModelError("", "This employee already has an active salary definition.");
                    ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive), "Id", "FullName", salaryDefinition.EmployeeId);
                     return View(salaryDefinition);
                }

                salaryDefinition.CreatedBy = User.Identity.Name;
                salaryDefinition.CreatedDate = DateTime.Now;
                salaryDefinition.IsActive = true;

                _context.Add(salaryDefinition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Salary definition created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive), "Id", "FullName", salaryDefinition.EmployeeId);
             return View(salaryDefinition);
        }

        // GET: SalaryDefinition/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salaryDefinition = await _context.SalaryDefinitions
                .Include(sd => sd.Employee)
                .FirstOrDefaultAsync(sd => sd.Id == id);

            if (salaryDefinition == null)
            {
                return NotFound();
            }

             return View(salaryDefinition);
        }

        // POST: SalaryDefinition/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SalaryDefinition salaryDefinition)
        {
            if (id != salaryDefinition.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Employee");
            ModelState.Remove("CreatedBy");
             if (ModelState.IsValid)
            {
                try
                {
                    var existingSalary = await _context.SalaryDefinitions.FindAsync(id);
                    if (existingSalary == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingSalary.BasicSalary = salaryDefinition.BasicSalary;
                    existingSalary.HouseRentAllowance = salaryDefinition.HouseRentAllowance;
                    existingSalary.MedicalAllowance = salaryDefinition.MedicalAllowance;
                    existingSalary.TransportationAllowance = salaryDefinition.TransportationAllowance;
                    existingSalary.OtherAllowances = salaryDefinition.OtherAllowances;
                    existingSalary.ProvidentFund = salaryDefinition.ProvidentFund;
                    existingSalary.TaxDeduction = salaryDefinition.TaxDeduction;
                    existingSalary.OtherDeductions = salaryDefinition.OtherDeductions;

                    existingSalary.ModifiedBy = User.Identity.Name;
                    existingSalary.ModifiedDate = DateTime.Now;

                    _context.Update(existingSalary);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Salary definition updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalaryDefinitionExists(salaryDefinition.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

             return View(salaryDefinition);
        }

        // GET: SalaryDefinition/Deactivate/5
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salaryDefinition = await _context.SalaryDefinitions
                .Include(sd => sd.Employee)
                .FirstOrDefaultAsync(sd => sd.Id == id);

            if (salaryDefinition == null)
            {
                return NotFound();
            }

            return View(salaryDefinition);
        }

        // POST: SalaryDefinition/Deactivate/5
        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(int id)
        {
            var salaryDefinition = await _context.SalaryDefinitions.FindAsync(id);
            if (salaryDefinition != null)
            {
                salaryDefinition.IsActive = false;
                salaryDefinition.ModifiedBy = User.Identity.Name;
                salaryDefinition.ModifiedDate = DateTime.Now;

                _context.Update(salaryDefinition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Salary definition deactivated successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SalaryDefinitionExists(int id)
        {
            return _context.SalaryDefinitions.Any(e => e.Id == id);
        }
    }
}
 