using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AcademicYearsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AcademicYearsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AcademicYears
        public async Task<IActionResult> Index()
        {
            // Get all classes with their current academic years (managed at Class level, not Section)
            var classes = await _context.Classes
                .Include(c => c.Campus)
                .Include(c => c.ClassSections)
                .Where(c => c.IsActive)
                .OrderBy(c => c.CampusId)
                .ThenBy(c => c.Name)
                .Select(c => new ClassAcademicYearViewModel
                {
                    ClassId = c.Id,
                    ClassName = c.Name,
                    CurrentAcademicYear = c.CurrentAcademicYear,
                    CampusId = c.CampusId,
                    CampusName = c.Campus.Name,
                    SectionCount = c.ClassSections.Count(cs => cs.IsActive)
                })
                .ToListAsync();

            // Get available academic years for dropdown
            var academicYears = await _context.AcademicYear
                .OrderByDescending(ay => ay.Year)
                .ToListAsync();
            
            ViewBag.AcademicYears = academicYears;

            return View(classes);
        }

        // POST: Update Academic Year for a Class
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAcademicYear([FromBody] UpdateAcademicYearRequest request)
        {
            try
            {
                var classEntity = await _context.Classes.FindAsync(request.ClassId);
                if (classEntity == null)
                {
                    return Json(new { success = false, message = "Class not found." });
                }

                // Update the class's current academic year
                classEntity.CurrentAcademicYear = request.NewAcademicYear;

                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Academic year updated successfully for the class!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: AcademicYears/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicYear = await _context.AcademicYear
                .FirstOrDefaultAsync(m => m.Id == id);
            if (academicYear == null)
            {
                return NotFound();
            }

            return View(academicYear);
        }

        // GET: AcademicYears/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AcademicYears/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Year")] AcademicYear academicYear)
        {
            if (ModelState.IsValid)
            {
                // Check if year already exists
                var exists = await _context.AcademicYear
                    .AnyAsync(ay => ay.Year == academicYear.Year);
                
                if (exists)
                {
                    ModelState.AddModelError("Year", "This academic year already exists.");
                    return View(academicYear);
                }

                _context.Add(academicYear);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic year created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(academicYear);
        }

        // GET: AcademicYears/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicYear = await _context.AcademicYear.FindAsync(id);
            if (academicYear == null)
            {
                return NotFound();
            }
            return View(academicYear);
        }

        // POST: AcademicYears/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Year")] AcademicYear academicYear)
        {
            if (id != academicYear.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if year already exists (excluding current record)
                    var exists = await _context.AcademicYear
                        .AnyAsync(ay => ay.Year == academicYear.Year && ay.Id != id);
                    
                    if (exists)
                    {
                        ModelState.AddModelError("Year", "This academic year already exists.");
                        return View(academicYear);
                    }

                    _context.Update(academicYear);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Academic year updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AcademicYearExists(academicYear.Id))
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
            return View(academicYear);
        }

        // GET: AcademicYears/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicYear = await _context.AcademicYear
                .FirstOrDefaultAsync(m => m.Id == id);
            if (academicYear == null)
            {
                return NotFound();
            }

            // Check if academic year is in use
            var isInUse = await IsAcademicYearInUse(academicYear.Year);
            if (isInUse)
            {
                TempData["ErrorMessage"] = "Cannot delete this academic year as it is currently in use by students, attendance, billing, or exam records.";
                return RedirectToAction(nameof(Index));
            }

            return View(academicYear);
        }

        // POST: AcademicYears/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var academicYear = await _context.AcademicYear.FindAsync(id);
            if (academicYear != null)
            {
                // Double check if academic year is in use
                var isInUse = await IsAcademicYearInUse(academicYear.Year);
                if (isInUse)
                {
                    TempData["ErrorMessage"] = "Cannot delete this academic year as it is currently in use.";
                    return RedirectToAction(nameof(Index));
                }

                _context.AcademicYear.Remove(academicYear);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic year deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AcademicYearExists(int id)
        {
            return _context.AcademicYear.Any(e => e.Id == id);
        }

        private async Task<bool> IsAcademicYearInUse(int year)
        {
            // Check if academic year is used in any tables
            var hasAttendance = await _context.Attendance.AnyAsync(a => a.AcademicYear == year);
            var hasBilling = await _context.BillingMaster.AnyAsync(b => b.AcademicYear == year);
            var hasExamMarks = await _context.ExamMarks.AnyAsync(e => e.AcademicYear == year);
            var hasStudentHistory = await _context.StudentHistories.AnyAsync(s => s.AcademicYear == year);
            var hasNamazAttendance = await _context.NamazAttendance.AnyAsync(n => n.AcademicYear == year);

            return hasAttendance || hasBilling || hasExamMarks || hasStudentHistory || hasNamazAttendance;
        }
    }
}
