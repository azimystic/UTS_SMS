using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class SubstitutionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubstitutionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Substitutions
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            // Default to current month and year
            var filterMonth = month ?? DateTime.Now.Month;
            var filterYear = year ?? DateTime.Now.Year;

            var substitutions = await _context.Substitutions
                .Include(s => s.TimetableSlot)
                    .ThenInclude(ts => ts.Timetable)
                    .ThenInclude(t => t.Class)
                .Include(s => s.TimetableSlot)
                    .ThenInclude(ts => ts.Timetable)
                    .ThenInclude(t => t.Section)
                .Include(s => s.TimetableSlot)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(s => s.OriginalTeacher)
                .Include(s => s.SubstituteEmployee)
                .Where(s => s.IsActive && 
                           s.CampusId == campusId &&
                           s.Date.Month == filterMonth && 
                           s.Date.Year == filterYear)
                .OrderByDescending(s => s.Date)
                .ThenBy(s => s.TimetableSlot.StartTime)
                .ToListAsync();

            ViewData["FilterMonth"] = filterMonth;
            ViewData["FilterYear"] = filterYear;
            ViewData["Months"] = GetMonths();

            return View(substitutions);
        }

        // GET: Substitutions/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            ViewData["Classes"] = await _context.Classes
                .Where(c => c.IsActive && c.CampusId == campusId)
                .ToListAsync();

            return View();
        }

        // GET: Substitutions/GetSectionsByClass
        [HttpGet]
        public async Task<IActionResult> GetSectionsByClass(int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !currentUser.CampusId.HasValue)
            {
                return Json(new List<object>());
            }
            
            var campusId = currentUser.CampusId.Value;

            var sections = await _context.ClassSections
                .Where(s => s.ClassId == classId && s.IsActive && s.CampusId == campusId)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            return Json(sections);
        }

        // GET: Substitutions/GetTimetable
        [HttpGet]
        public async Task<IActionResult> GetTimetable(int classId, int sectionId, DateTime date)
        {
            var timetable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .FirstOrDefaultAsync(t => t.ClassId == classId && 
                                         t.SectionId == sectionId && 
                                         t.IsActive);

            if (timetable == null)
            {
                return Json(new { success = false, message = "No timetable found for this class and section." });
            }

            // Get day of week (1 = Monday, 5 = Friday)
            int dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7
            if (dayOfWeek > 5)
            {
                return Json(new { success = false, message = "Please select a weekday (Monday to Friday)." });
            }

            // Get slots for the selected day, excluding breaks
            var slots = timetable.TimetableSlots
                .Where(ts => ts.DayOfWeek == dayOfWeek && !ts.IsBreak && !ts.IsZeroPeriod)
                .OrderBy(ts => ts.PeriodNumber)
                .Select(ts => new
                {
                    ts.Id,
                    ts.PeriodNumber,
                    StartTime = ts.StartTime.ToString("hh:mm tt"),
                    EndTime = ts.EndTime.ToString("hh:mm tt"),
                    TeacherName = ts.TeacherAssignment?.Teacher?.FullName ?? "Unassigned",
                    TeacherId = ts.TeacherAssignment?.TeacherId,
                    SubjectName = ts.TeacherAssignment?.Subject?.Name ?? "N/A",
                    HasAssignment = ts.TeacherAssignmentId.HasValue
                })
                .ToList();

            // Check for existing substitutions
            var existingSubstitutions = await _context.Substitutions
                .Where(s => s.Date.Date == date.Date && 
                           s.TimetableSlot.TimetableId == timetable.Id &&
                           s.IsActive)
                .Select(s => s.TimetableSlotId)
                .ToListAsync();

            return Json(new
            {
                success = true,
                timetableId = timetable.Id,
                className = timetable.Class.Name,
                sectionName = timetable.Section.Name,
                slots = slots,
                existingSubstitutions = existingSubstitutions
            });
        }

        // GET: Substitutions/GetAvailableEmployees
        [HttpGet]
        public async Task<IActionResult> GetAvailableEmployees(int timetableSlotId, DateTime date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var slot = await _context.TimetableSlots
                .Include(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .FirstOrDefaultAsync(ts => ts.Id == timetableSlotId);

            if (slot == null)
            {
                return Json(new { success = false, message = "Timetable slot not found." });
            }

            // Get the day of week
            int dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            // Get all employees from the campus
            var allEmployees = await _context.Employees
                .Where(e => e.IsActive && e.CampusId == campusId)
                .ToListAsync();

            // Get all teacher assignments that are busy during this time slot on this day
            var busyTeacherIds = await _context.TimetableSlots
                .Where(ts => ts.DayOfWeek == dayOfWeek &&
                           ts.TeacherAssignmentId.HasValue &&
                           // Check for time overlap
                           ((ts.StartTime >= slot.StartTime && ts.StartTime < slot.EndTime) ||
                            (ts.EndTime > slot.StartTime && ts.EndTime <= slot.EndTime) ||
                            (ts.StartTime <= slot.StartTime && ts.EndTime >= slot.EndTime)))
                .Select(ts => ts.TeacherAssignment.TeacherId)
                .Distinct()
                .ToListAsync();

            // Filter available employees
            var availableEmployees = allEmployees
                .Where(e => !busyTeacherIds.Contains(e.Id))
                .Where(e => {
                    // Check employee availability based on OnTime and OffTime
                    if (e.OnTime.HasValue && e.OffTime.HasValue)
                    {
                        var slotStartTime = TimeOnly.FromDateTime(slot.StartTime);
                        var slotEndTime = TimeOnly.FromDateTime(slot.EndTime);
                        return slotStartTime >= e.OnTime.Value && slotEndTime <= e.OffTime.Value;
                    }
                    return true; // If no time constraints, consider available
                })
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Role,
                    OnTime = e.OnTime.HasValue ? e.OnTime.Value.ToString("hh:mm tt") : "N/A",
                    OffTime = e.OffTime.HasValue ? e.OffTime.Value.ToString("hh:mm tt") : "N/A"
                })
                .OrderBy(e => e.FullName)
                .ToList();

            var originalTeacher = slot.TeacherAssignment?.Teacher;

            return Json(new
            {
                success = true,
                slotInfo = new
                {
                    PeriodNumber = slot.PeriodNumber,
                    StartTime = slot.StartTime.ToString("hh:mm tt"),
                    EndTime = slot.EndTime.ToString("hh:mm tt"),
                    OriginalTeacher = originalTeacher?.FullName ?? "Unassigned",
                    OriginalTeacherId = originalTeacher?.Id
                },
                availableEmployees = availableEmployees
            });
        }

        // POST: Substitutions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int timetableSlotId, DateTime date, int substituteEmployeeId, string reason)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var slot = await _context.TimetableSlots
                .Include(ts => ts.TeacherAssignment)
                .FirstOrDefaultAsync(ts => ts.Id == timetableSlotId);

            if (slot == null || !slot.TeacherAssignmentId.HasValue)
            {
                TempData["Error"] = "Invalid timetable slot or no teacher assigned.";
                return RedirectToAction(nameof(Create));
            }

            // Check if substitution already exists
            var existingSubstitution = await _context.Substitutions
                .FirstOrDefaultAsync(s => s.TimetableSlotId == timetableSlotId && 
                                         s.Date.Date == date.Date && 
                                         s.IsActive);

            if (existingSubstitution != null)
            {
                TempData["Error"] = "A substitution already exists for this slot and date.";
                return RedirectToAction(nameof(Create));
            }

            var substitution = new Substitution
            {
                TimetableSlotId = timetableSlotId,
                Date = date.Date,
                OriginalTeacherId = slot.TeacherAssignment.TeacherId,
                SubstituteEmployeeId = substituteEmployeeId,
                Reason = reason,
                CampusId = (int)campusId,
                CreatedBy = currentUser.UserName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.Substitutions.Add(substitution);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Substitution created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Substitutions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var substitution = await _context.Substitutions
                .Include(s => s.TimetableSlot)
                    .ThenInclude(ts => ts.Timetable)
                    .ThenInclude(t => t.Class)
                .Include(s => s.TimetableSlot)
                    .ThenInclude(ts => ts.Timetable)
                    .ThenInclude(t => t.Section)
                .Include(s => s.TimetableSlot)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(s => s.OriginalTeacher)
                .Include(s => s.SubstituteEmployee)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (substitution == null)
            {
                return NotFound();
            }

            // Get available employees for the time slot
            var dayOfWeek = (int)substitution.Date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var slot = substitution.TimetableSlot;
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var allEmployees = await _context.Employees
                .Where(e => e.IsActive && e.CampusId == campusId)
                .ToListAsync();

            var busyTeacherIds = await _context.TimetableSlots
                .Where(ts => ts.DayOfWeek == dayOfWeek &&
                           ts.TeacherAssignmentId.HasValue &&
                           ts.Id != slot.Id && // Exclude current slot
                           ((ts.StartTime >= slot.StartTime && ts.StartTime < slot.EndTime) ||
                            (ts.EndTime > slot.StartTime && ts.EndTime <= slot.EndTime) ||
                            (ts.StartTime <= slot.StartTime && ts.EndTime >= slot.EndTime)))
                .Select(ts => ts.TeacherAssignment.TeacherId)
                .Distinct()
                .ToListAsync();

            var availableEmployees = allEmployees
                .Where(e => !busyTeacherIds.Contains(e.Id) || e.Id == substitution.SubstituteEmployeeId)
                .Where(e => {
                    if (e.OnTime.HasValue && e.OffTime.HasValue)
                    {
                        var slotStartTime = TimeOnly.FromDateTime(slot.StartTime);
                        var slotEndTime = TimeOnly.FromDateTime(slot.EndTime);
                        return slotStartTime >= e.OnTime.Value && slotEndTime <= e.OffTime.Value;
                    }
                    return true;
                })
                .ToList();

            ViewData["AvailableEmployees"] = availableEmployees;

            return View(substitution);
        }

        // POST: Substitutions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int substituteEmployeeId, string reason)
        {
            var substitution = await _context.Substitutions.FindAsync(id);

            if (substitution == null)
            {
                return NotFound();
            }

            substitution.SubstituteEmployeeId = substituteEmployeeId;
            substitution.Reason = reason;

            try
            {
                _context.Update(substitution);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Substitution updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubstitutionExists(substitution.Id))
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

        // POST: Substitutions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var substitution = await _context.Substitutions.FindAsync(id);
            if (substitution != null)
            {
                substitution.IsActive = false;
                _context.Update(substitution);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Substitution deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SubstitutionExists(int id)
        {
            return _context.Substitutions.Any(e => e.Id == id);
        }

        private List<object> GetMonths()
        {
            return new List<object>
            {
                new { Value = 1, Text = "January" },
                new { Value = 2, Text = "February" },
                new { Value = 3, Text = "March" },
                new { Value = 4, Text = "April" },
                new { Value = 5, Text = "May" },
                new { Value = 6, Text = "June" },
                new { Value = 7, Text = "July" },
                new { Value = 8, Text = "August" },
                new { Value = 9, Text = "September" },
                new { Value = 10, Text = "October" },
                new { Value = 11, Text = "November" },
                new { Value = 12, Text = "December" }
            };
        }
    }
}
