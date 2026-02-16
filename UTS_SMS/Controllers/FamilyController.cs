using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class FamilyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public FamilyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: Family
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            IQueryable<Family> familiesQuery = _context.Families
                .Include(f => f.Campus)
                .Include(f => f.Students)
                .Where(f => f.IsActive);

            if (campusId.HasValue && campusId > 0)
            {
                familiesQuery = familiesQuery.Where(f => f.CampusId == campusId);
            }

            var families = await familiesQuery.OrderBy(f => f.FatherName).ToListAsync();

            ViewBag.Campuses = campusId.HasValue && campusId > 0 
                ? await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync()
                : await _context.Campuses.Where(c => c.IsActive).ToListAsync();

            return View(families);
        }

        // GET: Family/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var family = await _context.Families
                .Include(f => f.Campus)
                .Include(f => f.Students)
                    .ThenInclude(s => s.ClassObj)
                .Include(f => f.Students)
                    .ThenInclude(s => s.SectionObj)
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (family == null)
                return NotFound();

            return View(family);
        }

        // GET: Family/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var family = await _context.Families.FindAsync(id);
            if (family == null || !family.IsActive)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CampusId"] = campusId.HasValue && campusId > 0
                ? new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", family.CampusId)
                : new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", family.CampusId);

            return View(family);
        }

        // POST: Family/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FatherName,FatherCNIC,FatherPhone,HomeAddress,FatherSourceOfIncome,IsFatherDeceased,GuardianName,GuardianPhone,CampusId")] Family family)
        {
            if (id != family.Id)
                return NotFound();

            ModelState.Remove("Students");
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingFamily = await _context.Families.FindAsync(id);
                    if (existingFamily == null)
                        return NotFound();

                    existingFamily.FatherName = family.FatherName;
                    existingFamily.FatherPhone = family.FatherPhone;
                    existingFamily.HomeAddress = family.HomeAddress;
                    existingFamily.FatherSourceOfIncome = family.FatherSourceOfIncome;
                    existingFamily.IsFatherDeceased = family.IsFatherDeceased;
                    existingFamily.GuardianName = family.GuardianName;
                    existingFamily.GuardianPhone = family.GuardianPhone;
                    existingFamily.ModifiedDate = DateTime.Now;
                    existingFamily.ModifiedBy = User.Identity?.Name;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FamilyExists(family.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CampusId"] = campusId.HasValue && campusId > 0
                ? new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", family.CampusId)
                : new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", family.CampusId);

            return View(family);
        }

        // GET: Family/StudentDetails/5
        public async Task<IActionResult> StudentDetails(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Family)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            // Get student's data for the last month
            var lastMonth = DateTime.Now.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Attendance data
            var attendanceData = await _context.Attendance
                .Where(a => a.StudentId == id && a.Date >= startDate && a.Date <= endDate)
                .GroupBy(a => a.Date.Date)
                .Select(g => new { Date = g.Key, Present = g.Any(a => a.Status == "P") })
                .OrderBy(a => a.Date)
                .ToListAsync();

            // Fee record
            var feeRecord = await _context.BillingMaster
                .Where(b => b.StudentId == id && b.ForMonth == lastMonth.Month && b.ForYear == lastMonth.Year)
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync();

            // Test results
            var testResults = await _context.ExamMarks
                .Where(em => em.StudentId == id && em.CreatedDate >= startDate && em.CreatedDate <= endDate)
                .Include(em => em.Exam)
                .Include(em => em.Subject)
                .OrderByDescending(em => em.CreatedDate)
                .ToListAsync();

            // Namaz attendance
            var namazAttendance = await _context.NamazAttendance
                .Where(na => na.StudentId == id && na.Date >= startDate && na.Date <= endDate)
                .OrderBy(na => na.Date)
                .ToListAsync();

            ViewBag.AttendanceData = attendanceData;
            ViewBag.FeeRecord = feeRecord;
            ViewBag.TestResults = testResults;
            ViewBag.NamazAttendance = namazAttendance;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(student);
        }

        private bool FamilyExists(int id)
        {
            return _context.Families.Any(e => e.Id == id);
        }

        // Method to auto-create families when students are enrolled
        public static async Task<Family?> CreateFamilyIfNotExists(ApplicationDbContext context, Student student, string? createdBy)
        {
            // Check if family already exists for this Father CNIC
            var existingFamily = await context.Families
                .FirstOrDefaultAsync(f => f.FatherCNIC == student.FatherCNIC && f.IsActive);

            if (existingFamily != null)
            {
                return existingFamily;
            }

            // Create new family
            var newFamily = new Family
            {
                FatherName = student.FatherName,
                FatherCNIC = student.FatherCNIC,
                FatherPhone = student.FatherPhone,
                HomeAddress = student.HomeAddress,
                FatherSourceOfIncome = student.FatherSourceOfIncome,
                IsFatherDeceased = student.IsFatherDeceased,
                GuardianName = student.GuardianName,
                GuardianPhone = student.GuardianPhone,
                CampusId = student.CampusId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.Now
            };

            context.Families.Add(newFamily);
            await context.SaveChangesAsync();

            return newFamily;
        }
    }
}