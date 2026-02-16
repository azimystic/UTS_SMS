using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentFineChargesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentFineChargesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentFineCharges
        public async Task<IActionResult> Index(string filter = "all", string searchString = "", int? classFilter = null, int? sectionFilter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            ViewBag.Classes = await _context.Classes
                .Where(c => c.IsActive && (campusId == null || c.CampusId == campusId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Sections = await _context.ClassSections
                .Where(cs => cs.IsActive && (campusId == null || cs.CampusId == campusId))
                .OrderBy(cs => cs.Name)
                .ToListAsync();

            ViewData["Filter"] = filter;
            ViewData["SearchString"] = searchString;
            ViewData["ClassFilter"] = classFilter;
            ViewData["SectionFilter"] = sectionFilter;

            var query = _context.StudentFineCharges
                .Include(sfc => sfc.Student)
                    .ThenInclude(s => s.ClassObj)
                .Include(sfc => sfc.Student)
                    .ThenInclude(s => s.SectionObj)
                .Include(sfc => sfc.Campus)
                .Where(sfc => sfc.IsActive && (campusId == null || sfc.CampusId == campusId));

            // Apply filter
            if (filter == "paid")
            {
                query = query.Where(sfc => sfc.IsPaid);
            }
            else if (filter == "unpaid")
            {
                query = query.Where(sfc => !sfc.IsPaid);
            }

            // Apply search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(sfc =>
                    sfc.Student.StudentName.Contains(searchString) ||
                    sfc.Student.StudentCNIC.Contains(searchString) ||
                    sfc.Student.FatherCNIC.Contains(searchString) ||
                    sfc.ChargeName.Contains(searchString));
            }

            // Apply class filter
            if (classFilter.HasValue)
            {
                query = query.Where(sfc => sfc.Student.Class == classFilter.Value);
            }

            // Apply section filter
            if (sectionFilter.HasValue)
            {
                query = query.Where(sfc => sfc.Student.Section == sectionFilter.Value);
            }

            var fineCharges = await query
                .OrderByDescending(sfc => sfc.ChargeDate)
                .ToListAsync();

            return View(fineCharges);
        }

        // GET: StudentFineCharges/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            ViewBag.Classes = await _context.Classes
                .Where(c => c.IsActive && (campusId == null || c.CampusId == campusId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Templates = await _context.FineChargeTemplates
                .Where(t => t.IsActive && (campusId == null || t.CampusId == campusId))
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View();
        }

        // POST: StudentFineCharges/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int studentId, List<StudentFineChargeDto> charges)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId ?? 0;

            if (charges == null || !charges.Any())
            {
                TempData["ErrorMessage"] = "Please add at least one charge.";
                return RedirectToAction(nameof(Create));
            }

            foreach (var charge in charges)
            {
                var fineCharge = new StudentFineCharge
                {
                    StudentId = studentId,
                    ChargeName = charge.ChargeName,
                    Amount = charge.Amount,
                    Description = charge.Description,
                    ChargeDate = DateTime.Now,
                    IsPaid = false,
                    CampusId = campusId,
                    CreatedBy = currentUser.FullName,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.StudentFineCharges.Add(fineCharge);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{charges.Count} charge(s) added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: StudentFineCharges/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var studentId = id;

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == studentId && (campusId == null || s.CampusId == campusId));

            if (student == null)
            {
                return NotFound();
            }

            var fineCharges = await _context.StudentFineCharges
                .Where(sfc => sfc.StudentId == studentId && sfc.IsActive)
                .OrderByDescending(sfc => sfc.ChargeDate)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.Templates = await _context.FineChargeTemplates
                .Where(t => t.IsActive && (campusId == null || t.CampusId == campusId))
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(fineCharges);
        }

        // POST: StudentFineCharges/AddCharge
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCharge(int studentId, string chargeName, decimal amount, string? description)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId ?? 0;

            var fineCharge = new StudentFineCharge
            {
                StudentId = studentId,
                ChargeName = chargeName,
                Amount = amount,
                Description = description,
                ChargeDate = DateTime.Now,
                IsPaid = false,
                CampusId = campusId,
                CreatedBy = currentUser.FullName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.StudentFineCharges.Add(fineCharge);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Charge added successfully.";
            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        // POST: StudentFineCharges/DeleteCharge/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCharge(int id)
        {
            var fineCharge = await _context.StudentFineCharges.FindAsync(id);

            if (fineCharge == null)
            {
                return NotFound();
            }

            if (fineCharge.IsPaid)
            {
                TempData["ErrorMessage"] = "Cannot delete a paid charge.";
                return RedirectToAction(nameof(Details), new { id = fineCharge.StudentId });
            }

            _context.StudentFineCharges.Remove(fineCharge);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Charge deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = fineCharge.StudentId });
        }

        // POST: StudentFineCharges/TogglePaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePaid(int id)
        {
            var fineCharge = await _context.StudentFineCharges.FindAsync(id);

            if (fineCharge == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            fineCharge.IsPaid = !fineCharge.IsPaid;
            fineCharge.PaidDate = fineCharge.IsPaid ? DateTime.Now : null;
            fineCharge.ModifiedBy = currentUser.FullName;
            fineCharge.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Charge marked as {(fineCharge.IsPaid ? "paid" : "unpaid")}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: StudentFineCharges/SearchStudents
        public async Task<JsonResult> SearchStudents(string searchTerm, int? classId, int? sectionId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var query = _context.Students
                .Where(s => !s.HasLeft && (campusId == null || s.CampusId == campusId));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    s.StudentName.Contains(searchTerm) ||
                    s.FatherName.Contains(searchTerm) ||
                    (s.StudentCNIC != null && s.StudentCNIC.Contains(searchTerm)) ||
                    (s.FatherCNIC != null && s.FatherCNIC.Contains(searchTerm)));
            }

            if (classId.HasValue)
            {
                query = query.Where(s => s.Class == classId.Value);
            }

            if (sectionId.HasValue)
            {
                query = query.Where(s => s.Section == sectionId.Value);
            }

            var students = await query
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .OrderBy(s => s.StudentName)
                .Take(20)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.StudentName,
                    fatherName = s.FatherName,
                    className = s.ClassObj.Name,
                    sectionName = s.SectionObj.Name,
                    studentCnic = s.StudentCNIC ?? "",
                    fatherCnic = s.FatherCNIC ?? ""
                })
                .ToListAsync();

            return Json(students);
        }

        // GET: StudentFineCharges/GetStudentFines/5
        public async Task<JsonResult> GetStudentFines(int studentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var fines = await _context.StudentFineCharges
                .Where(sfc => sfc.StudentId == studentId && sfc.IsActive && (campusId == null || sfc.CampusId == campusId))
                .OrderByDescending(sfc => sfc.ChargeDate)
                .Select(sfc => new
                {
                    id = sfc.Id,
                    chargeName = sfc.ChargeName,
                    amount = sfc.Amount,
                    description = sfc.Description,
                    chargeDate = sfc.ChargeDate.ToString("dd MMM yyyy"),
                    isPaid = sfc.IsPaid,
                    paidDate = sfc.PaidDate.HasValue ? sfc.PaidDate.Value.ToString("dd MMM yyyy") : null
                })
                .ToListAsync();

            return Json(fines);
        }

        // GET: StudentFineCharges/GetUnpaidCharges/5
        public async Task<JsonResult> GetUnpaidCharges(int studentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var unpaidCharges = await _context.StudentFineCharges
                .Where(sfc => sfc.StudentId == studentId && !sfc.IsPaid && sfc.IsActive && (campusId == null || sfc.CampusId == campusId))
                .Select(sfc => new
                {
                    id = sfc.Id,
                    chargeName = sfc.ChargeName,
                    amount = sfc.Amount,
                    chargeDate = sfc.ChargeDate.ToString("dd MMM yyyy")
                })
                .ToListAsync();

            return Json(unpaidCharges);
        }

        // Templates Management
        // GET: StudentFineCharges/Templates
        public async Task<IActionResult> Templates()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var templates = await _context.FineChargeTemplates
                .Where(t => t.IsActive && (campusId == null || t.CampusId == campusId))
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(templates);
        }

        // POST: StudentFineCharges/CreateTemplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTemplate(string name, decimal amount, string? description)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId ?? 0;

            var template = new FineChargeTemplate
            {
                Name = name,
                Amount = amount,
                Description = description,
                CampusId = campusId,
                CreatedBy = currentUser.FullName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.FineChargeTemplates.Add(template);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Template created successfully.";
            return RedirectToAction(nameof(Templates));
        }

        // POST: StudentFineCharges/DeleteTemplate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.FineChargeTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            template.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Template deleted successfully.";
            return RedirectToAction(nameof(Templates));
        }
    }

    // DTO for creating charges
    public class StudentFineChargeDto
    {
        public string ChargeName { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}
