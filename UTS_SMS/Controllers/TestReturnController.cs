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
    public class TestReturnController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;

        public TestReturnController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        // GET: TestReturn
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            // Default to current month/year if not provided
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            var query = _context.TestReturns
                .Include(t => t.Exam)
                .Include(t => t.Subject)
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.Teacher)
                .Include(t => t.Campus)
                .Where(t => t.IsActive && t.ExamDate.Month == currentMonth && t.ExamDate.Year == currentYear);

            // Filter by campus if user is not admin
            if (campusId.HasValue)
            {
                query = query.Where(t => t.CampusId == campusId.Value);
            }

            var testReturns = await query
                .OrderByDescending(t => t.ExamDate)
                .ToListAsync();

            // Prepare filter options
            ViewBag.CurrentMonth = currentMonth;
            ViewBag.CurrentYear = currentYear;
            ViewBag.MonthOptions = Enumerable.Range(1, 12).Select(m => new SelectListItem
            {
                Value = m.ToString(),
                Text = new DateTime(2000, m, 1).ToString("MMMM")
            }).ToList();

            ViewBag.YearOptions = Enumerable.Range(DateTime.Now.Year - 2, 5).Select(y => new SelectListItem
            {
                Value = y.ToString(),
                Text = y.ToString()
            }).ToList();

            // Campus list for admin
            if (!campusId.HasValue)
            {
                ViewBag.CampusList = await _context.Campuses
                    .Where(c => c.IsActive)
                    .ToListAsync();
            }

            return View(testReturns);
        }

        // GET: TestReturn/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var testReturn = await _context.TestReturns
                .Include(t => t.Exam)
                .Include(t => t.Subject)
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.Teacher)
                .Include(t => t.Campus)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testReturn == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && testReturn.CampusId != currentUser.CampusId.Value)
                return Forbid();

            return View(testReturn);
        }

        // GET: TestReturn/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            
            await PopulateDropDowns(currentUser?.CampusId, isAdmin);
            
            var model = new TestReturn
            {
                ExamDate = DateTime.Now,
                CampusId = currentUser?.CampusId ?? 0
            };
            
            // If user is a teacher, auto-populate the TeacherId
            if (!isAdmin && currentUser != null)
            {
                var teacher = await _context.Employees
                    .FirstOrDefaultAsync(e => e.CNIC == currentUser.UserName && e.Role == "Teacher" && e.IsActive);
                if (teacher != null)
                {
                    model.TeacherId = teacher.Id;
                }
            }
            
            ViewBag.IsAdmin = isAdmin;
            
            return View(model);
        }

        // POST: TestReturn/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ExamCategoryId,ExamId,SubjectId,ClassId,SectionId,TeacherId,ReturnDate,CheckingQuality,Remarks,CampusId")] TestReturn testReturn)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            
            if (currentUser?.CampusId.HasValue == true && testReturn.CampusId != currentUser.CampusId.Value)
                return Forbid();

            // If user is a teacher, auto-populate the TeacherId
            if (!isAdmin && currentUser != null)
            {
                var teacher = await _context.Employees
                    .FirstOrDefaultAsync(e => e.CNIC == currentUser.UserName && e.Role == "Teacher" && e.IsActive);
                if (teacher != null)
                {
                    testReturn.TeacherId = teacher.Id;
                }
            }

            // Fetch ExamDate from ExamDateSheet based on Exam, Subject, Class, and Section
            var examDateSheet = await _context.ExamDateSheets
                .Include(eds => eds.ClassSections)
                .FirstOrDefaultAsync(eds => 
                    eds.ExamId == testReturn.ExamId &&
                    eds.SubjectId == testReturn.SubjectId &&
                    eds.ClassSections.Any(cs => cs.ClassId == testReturn.ClassId && cs.SectionId == testReturn.SectionId) &&
                    eds.IsActive);

            if (examDateSheet == null)
            {
                ModelState.AddModelError("", "Exam date not found in the exam date sheet for the selected combination.");
                await PopulateDropDowns(currentUser?.CampusId, isAdmin);
                ViewBag.IsAdmin = isAdmin;
                return View(testReturn);
            }

            testReturn.ExamDate = examDateSheet.ExamDate;

            // Calculate IsReturnedOnTime based on ReturnDate and TestReturnDayFlexibility
            var testReturnDayFlexibility = _configuration.GetValue<int>("TestReturnDayFlexibility", 3);
            bool shouldSendLateNotification = false;
            int daysDifference = 0;
            
            if (testReturn.ReturnDate.HasValue)
            {
                daysDifference = (testReturn.ReturnDate.Value.Date - testReturn.ExamDate.Date).Days;
                testReturn.IsReturnedOnTime = daysDifference <= testReturnDayFlexibility;
                shouldSendLateNotification = !testReturn.IsReturnedOnTime;
            }
            else
            {
                // If no return date is provided, mark as not returned on time
                testReturn.IsReturnedOnTime = false;
            }

            ModelState.Remove("Exam");
            ModelState.Remove("Class");
            ModelState.Remove("Section");
            ModelState.Remove("Teacher");
            ModelState.Remove("Campus");
            ModelState.Remove("Subject");
            
            if (ModelState.IsValid)
            {
                testReturn.CreatedBy = currentUser?.FullName;
                testReturn.CreatedDate = DateTime.Now;
                
                _context.Add(testReturn);
                await _context.SaveChangesAsync();
                
                // Send notification to admin if late
                if (shouldSendLateNotification && testReturn.ReturnDate.HasValue)
                {
                    var teacher = await _context.Employees.FindAsync(testReturn.TeacherId);
                    var exam = await _context.Exams.FindAsync(testReturn.ExamId);
                    var subject = await _context.Subjects.FindAsync(testReturn.SubjectId);
                    var classEntity = await _context.Classes.FindAsync(testReturn.ClassId);
                    var section = await _context.ClassSections.FindAsync(testReturn.SectionId);

                    if (teacher != null && exam != null && subject != null && classEntity != null && section != null)
                    {
                        await _notificationService.CreateGeneralNotification(
                            "Late Test Return",
                            $"{teacher.FullName} returned {subject.Name} test papers for {classEntity.Name}-{section.Name} ({exam.Name}) late. " +
                            $"Exam Date: {testReturn.ExamDate:dd/MM/yyyy}, Return Date: {testReturn.ReturnDate.Value:dd/MM/yyyy} ({daysDifference} days late).",
                            "Admin",
                            testReturn.CampusId,
                            $"/TestReturn/Details/{testReturn.Id}"
                        );
                    }
                }
                
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(currentUser?.CampusId, isAdmin);
            ViewBag.IsAdmin = isAdmin;
            return View(testReturn);
        }

        // GET: TestReturn/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var testReturn = await _context.TestReturns.FindAsync(id);
            if (testReturn == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && testReturn.CampusId != currentUser.CampusId.Value)
                return Forbid();

            var isAdmin = User.IsInRole("Admin");
            await PopulateDropDowns(currentUser?.CampusId, isAdmin, testReturn);
            return View(testReturn);
        }

        // POST: TestReturn/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ExamId,SubjectId,ClassId,SectionId,TeacherId,ExamDate,ReturnDate,IsReturnedOnTime,CheckingQuality,Remarks,CampusId,CreatedDate,CreatedBy")] TestReturn testReturn)
        {
            if (id != testReturn.Id)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && testReturn.CampusId != currentUser.CampusId.Value)
                return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    testReturn.ModifiedBy = currentUser?.FullName;
                    testReturn.ModifiedDate = DateTime.Now;
                    
                    _context.Update(testReturn);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestReturnExists(testReturn.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var isAdmin = User.IsInRole("Admin");
            await PopulateDropDowns(currentUser?.CampusId, isAdmin, testReturn);
            return View(testReturn);
        }

        // GET: TestReturn/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var testReturn = await _context.TestReturns
                .Include(t => t.Exam)
                .Include(t => t.Subject)
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.Teacher)
                .Include(t => t.Campus)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testReturn == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && testReturn.CampusId != currentUser.CampusId.Value)
                return Forbid();

            return View(testReturn);
        }

        // POST: TestReturn/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testReturn = await _context.TestReturns.FindAsync(id);
            if (testReturn != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.CampusId.HasValue == true && testReturn.CampusId != currentUser.CampusId.Value)
                    return Forbid();

                testReturn.IsActive = false;
                testReturn.ModifiedBy = currentUser?.FullName;
                testReturn.ModifiedDate = DateTime.Now;
                
                _context.Update(testReturn);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TestReturnExists(int id)
        {
            return _context.TestReturns.Any(e => e.Id == id);
        }

        private async Task PopulateDropDowns(int? campusId, bool isAdmin = true, TestReturn? testReturn = null)
        {
            var examCategoryQuery = _context.ExamCategories.Where(ec => ec.IsActive);
            var examQuery = _context.Exams.Where(e => e.IsActive);
            var teacherQuery = _context.Employees.Where(e => e.IsActive && e.Role == "Teacher");
            var classQuery = _context.Classes.Where(c => c.IsActive);
            var subjectQuery = _context.Subjects.Where(s => s.IsActive);

            if (campusId.HasValue)
            {
                examCategoryQuery = examCategoryQuery.Where(ec => ec.CampusId == campusId.Value || ec.CampusId == null);
                examQuery = examQuery.Where(e => e.CampusId == campusId.Value || e.CampusId == null);
                teacherQuery = teacherQuery.Where(e => e.CampusId == campusId.Value);
                classQuery = classQuery.Where(c => c.CampusId == campusId.Value);
            }

            ViewData["ExamCategoryId"] = new SelectList(await examCategoryQuery.ToListAsync(), "Id", "Name", testReturn?.ExamId);
            ViewData["ExamId"] = new SelectList(await examQuery.ToListAsync(), "Id", "Name", testReturn?.ExamId);
            
            // Only populate teacher dropdown for admin users
            if (isAdmin)
            {
                ViewData["TeacherId"] = new SelectList(await teacherQuery.ToListAsync(), "Id", "FullName", testReturn?.TeacherId);
            }
            
            ViewData["ClassId"] = new SelectList(await classQuery.ToListAsync(), "Id", "Name", testReturn?.ClassId);
            ViewData["SubjectId"] = new SelectList(await subjectQuery.ToListAsync(), "Id", "Name", testReturn?.SubjectId);

            // Sections will be populated via JavaScript based on class selection
            ViewData["SectionId"] = new SelectList(new List<ClassSection>(), "Id", "Name", testReturn?.SectionId);

            // Campus dropdown for admin users
            if (!campusId.HasValue)
            {
                ViewData["CampusId"] = new SelectList(
                    await _context.Campuses.Where(c => c.IsActive).ToListAsync(), 
                    "Id", 
                    "Name", 
                    testReturn?.CampusId
                );
            }
        }

        // AJAX endpoint to get sections by class
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(s => s.ClassId == classId && s.IsActive)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(sections);
        }

        // AJAX endpoint to get exams by category
        [HttpGet]
        public async Task<JsonResult> GetExamsByCategory(int categoryId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var examQuery = _context.Exams
                .Where(e => e.ExamCategoryId == categoryId && e.IsActive);

            if (campusId.HasValue)
            {
                examQuery = examQuery.Where(e => e.CampusId == campusId.Value || e.CampusId == null);
            }

            var exams = await examQuery
                .Select(e => new { id = e.Id, name = e.Name })
                .ToListAsync();

            return Json(exams);
        }
    }
}