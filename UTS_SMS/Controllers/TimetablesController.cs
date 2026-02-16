// TimetablesController.cs
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
    public class TimetablesController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        
        // Constants for special period numbers
        private const int ZERO_PERIOD_NUMBER = 0;
        private const int BREAK_PERIOD_BASE = 999; // Break periods start from 999

        public TimetablesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        // GET: Timetables
        public async Task<IActionResult> Index()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            var timetablesQuery = _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Where(t => t.IsActive);

            // Filter by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                timetablesQuery = timetablesQuery.Where(t => t.CampusId == userCampusId.Value);
            }

            var timetables = await timetablesQuery.ToListAsync();

            return View(timetables);
        }

        // GET: Timetables/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timetable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(t => t.TimetableBreaks)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (timetable == null)
            {
                return NotFound();
            }

            // Order slots by day and period
            timetable.TimetableSlots = timetable.TimetableSlots
                .OrderBy(ts => ts.DayOfWeek)
                .ThenBy(ts => ts.PeriodNumber)
                .ToList();

            return View(timetable);
        }

        // GET: Timetables/Create
        public async Task<IActionResult> Create()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Filter dropdowns by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewData["Classes"] = _context.Classes.Where(c => c.IsActive && c.CampusId == userCampusId.Value).ToList();
                ViewData["Sections"] = _context.ClassSections.Where(s => s.IsActive && s.CampusId == userCampusId.Value).ToList();
            }
            else
            {
                ViewData["Classes"] = _context.Classes.Where(c => c.IsActive).ToList();
                ViewData["Sections"] = _context.ClassSections.Where(s => s.IsActive).ToList();
            }

            return View();
        }

        // POST: Timetables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,SectionId,StartTime,NumberOfLectures,LectureDuration,BreakDuration,BreakAfterPeriod,ZeroPeriodDuration")] Timetable timetable)
        {
            // Check if timetable already exists for this class and section
            var existingTimetable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.ClassId == timetable.ClassId &&
                                         t.SectionId == timetable.SectionId &&
                                         t.IsActive);

            if (existingTimetable != null)
            {
                ModelState.AddModelError("", "A timetable already exists for this class and section.");
            }
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            ModelState.Remove("Class");
            ModelState.Remove("Section");
            ModelState.Remove("TimetableSlots");
            ModelState.Remove("Campus");
            ModelState.Remove("TimetableBreaks");
            if (ModelState.IsValid)
            {
                // Set default break after period if not specified
                if (timetable.BreakAfterPeriod == 0)
                {
                    timetable.BreakAfterPeriod = timetable.NumberOfLectures / 2;
                }

                // Calculate break and zero period times
                if (timetable.BreakDuration > 0)
                {
                    timetable.BreakStartTime = timetable.StartTime.AddMinutes(timetable.BreakAfterPeriod * timetable.LectureDuration);
                }

                if (timetable.ZeroPeriodDuration > 0)
                {
                    timetable.ZeroPeriodStartTime = timetable.StartTime.AddMinutes(-timetable.ZeroPeriodDuration);
                }
                timetable.CampusId = (int)campusId;
                _context.Add(timetable);
                await _context.SaveChangesAsync();

                // Generate timetable slots
                await GenerateTimetableSlots(timetable.Id);

                TempData["SuccessMessage"] = "Timetable created successfully! You can now assign teachers to periods.";
                return RedirectToAction(nameof(EditSlots), new { id = timetable.Id });
            }

            ViewData["Classes"] = _context.Classes.Where(c => c.IsActive && (campusId == null || c.CampusId == campusId)).ToList();
            ViewData["Sections"] = _context.ClassSections.Where(s => s.IsActive && (campusId == null || s.CampusId == campusId)).ToList();
            return View(timetable);
        }

        // GET: Timetables/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timetable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (timetable == null)
            {
                return NotFound();
            }

            // Get teacher assignments for this class and section
            var teacherAssignments = await _context.TeacherAssignments
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Subject)
                .Where(ta => ta.ClassId == timetable.ClassId &&
                            ta.SectionId == timetable.SectionId &&
                            ta.IsActive )
                .ToListAsync();

            ViewData["TeacherAssignments"] = teacherAssignments;
            return View(timetable);
        }

        // POST: Timetables/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassId,SectionId,StartTime,NumberOfLectures,LectureDuration,BreakDuration,BreakAfterPeriod,ZeroPeriodDuration,IsActive")] Timetable timetable)
        {
            if (id != timetable.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Campus");
            ModelState.Remove("Class");
            ModelState.Remove("Section");
            ModelState.Remove("TimetableSlots");
            ModelState.Remove("TimetableBreaks");
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;
            timetable.CampusId = (int)campusId;
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default break after period if not specified
                    if (timetable.BreakAfterPeriod == 0)
                    {
                        timetable.BreakAfterPeriod = timetable.NumberOfLectures / 2;
                    }

                    // Recalculate times
                    if (timetable.BreakDuration > 0)
                    {
                        timetable.BreakStartTime = timetable.StartTime.AddMinutes(timetable.BreakAfterPeriod * timetable.LectureDuration);
                    }
                    else
                    {
                        timetable.BreakStartTime = null;
                    }

                    if (timetable.ZeroPeriodDuration > 0)
                    {
                        timetable.ZeroPeriodStartTime = timetable.StartTime.AddMinutes(-timetable.ZeroPeriodDuration);
                    }
                    else
                    {
                        timetable.ZeroPeriodStartTime = null;
                    }

                    _context.Update(timetable);
                    await _context.SaveChangesAsync();

                    // Regenerate slots if structure changed
                    await RegenerateTimetableSlots(timetable.Id);

                    TempData["SuccessMessage"] = "Timetable updated successfully! You can continue editing teacher assignments.";
                    return RedirectToAction(nameof(EditSlots), new { id = timetable.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimetableExists(timetable.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Get teacher assignments for this class and section
            var teacherAssignments = await _context.TeacherAssignments
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Subject)
                .Where(ta => ta.ClassId == timetable.ClassId &&
                            ta.SectionId == timetable.SectionId &&
                            ta.IsActive  )
                .ToListAsync();

            ViewData["TeacherAssignments"] = teacherAssignments;
            return View(timetable);
        }

        // GET: Timetables/EditSlots/5
        public async Task<IActionResult> EditSlots(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timetable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(t => t.TimetableBreaks)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (timetable == null)
            {
                return NotFound();
            }

            // If no slots exist, generate them
            if (!timetable.TimetableSlots.Any())
            {
                await GenerateTimetableSlots(timetable.Id);
                // Reload timetable with slots
                timetable = await _context.Timetables
                    .Include(t => t.Class)
                    .Include(t => t.Section)
                    .Include(t => t.TimetableSlots)
                        .ThenInclude(ts => ts.TeacherAssignment)
                        .ThenInclude(ta => ta.Teacher)
                    .Include(t => t.TimetableSlots)
                        .ThenInclude(ts => ts.TeacherAssignment)
                        .ThenInclude(ta => ta.Subject)
                    .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
            }

            // Order slots by day and period
            timetable.TimetableSlots = timetable.TimetableSlots
                .OrderBy(ts => ts.DayOfWeek)
                .ThenBy(ts => ts.PeriodNumber)
                .ToList();

            // Get teacher assignments for this class and section
            var teacherAssignments = await _context.TeacherAssignments
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Subject)
                .Where(ta => ta.ClassId == timetable.ClassId &&
                            ta.SectionId == timetable.SectionId &&
                            ta.IsActive  )
                .ToListAsync();

            ViewData["TeacherAssignments"] = teacherAssignments;
            
            // Pass teacher availability data to view
            ViewData["TeacherAvailability"] = teacherAssignments
                .Where(ta => ta.Teacher.OnTime.HasValue && ta.Teacher.OffTime.HasValue)
                .ToDictionary(
                    ta => ta.Id,
                    ta => new { 
                        OnTime = ta.Teacher.OnTime.Value.ToString("HH:mm"), 
                        OffTime = ta.Teacher.OffTime.Value.ToString("HH:mm") 
                    }
                );
            
            return View(timetable);
        }

        // POST: Timetables/UpdateSlots
        [HttpPost]
        public async Task<IActionResult> UpdateSlots([FromBody] List<SlotUpdateRequest> updates)
        {
            if (updates == null || !updates.Any())
            {
                return BadRequest("No updates provided");
            }

            try
            {
                foreach (var update in updates)
                {
                    var slot = await _context.TimetableSlots.FindAsync(update.SlotId);
                    if (slot != null)
                    {
                        // Validate that the teacher assignment belongs to this class and section
                        if (update.TeacherAssignmentId.HasValue)
                        {
                            var timetable = await _context.Timetables
                                .Include(t => t.Class)
                                .Include(t => t.Section)
                                .FirstOrDefaultAsync(t => t.Id == slot.TimetableId);

                            var teacherAssignment = await _context.TeacherAssignments
                                .Include(ta => ta.Teacher)
                                .FirstOrDefaultAsync(ta => ta.Id == update.TeacherAssignmentId &&
                                                          ta.ClassId == timetable.ClassId &&
                                                          ta.SectionId == timetable.SectionId &&
                                                          ta.IsActive);

                            if (teacherAssignment == null)
                            {
                                return BadRequest($"Invalid teacher assignment for slot {update.SlotId}");
                            }

                            // Validate teacher availability (OnTime and OffTime)
                            var teacher = teacherAssignment.Teacher;
                            if (teacher.OnTime.HasValue && teacher.OffTime.HasValue)
                            {
                                var slotStartTime = TimeOnly.FromDateTime(slot.StartTime);
                                var slotEndTime = TimeOnly.FromDateTime(slot.EndTime);

                                if (slotStartTime < teacher.OnTime.Value || slotEndTime > teacher.OffTime.Value)
                                {
                                    return BadRequest($"Teacher {teacher.FullName} is not available during the selected time slot. Please assign a teacher within their working hours ({teacher.OnTime.Value:hh\\:mm tt} - {teacher.OffTime.Value:hh\\:mm tt}).");
                                }
                            }
                        }

                        slot.TeacherAssignmentId = update.TeacherAssignmentId;
                        _context.Update(slot);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating slots: {ex.Message}");
            }
        }

        // POST: Timetables/UpdateBreakConfig
        [HttpPost]
        public async Task<IActionResult> UpdateBreakConfig(int timetableId, int breakAfterPeriod, int breakDuration)
        {
            try
            {
                var timetable = await _context.Timetables.FindAsync(timetableId);
                if (timetable == null)
                {
                    return NotFound("Timetable not found");
                }

                timetable.BreakAfterPeriod = breakAfterPeriod;
                timetable.BreakDuration = breakDuration;

                if (breakDuration > 0)
                {
                    timetable.BreakStartTime = timetable.StartTime.AddMinutes(breakAfterPeriod * timetable.LectureDuration);
                }
                else
                {
                    timetable.BreakStartTime = null;
                }

                _context.Update(timetable);
                await _context.SaveChangesAsync();

                // Regenerate slots with new break configuration
                await RegenerateTimetableSlots(timetableId);

                return Ok(new { success = true, message = "Break configuration updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating break configuration: {ex.Message}");
            }
        }

        // POST: Timetables/UpdateMultipleBreaks
        [HttpPost]
        public async Task<IActionResult> UpdateMultipleBreaks([FromBody] MultipleBreaksRequest request)
        {
            try
            {
                var timetable = await _context.Timetables
                    .Include(t => t.TimetableBreaks)
                    .FirstOrDefaultAsync(t => t.Id == request.TimetableId);
                    
                if (timetable == null)
                {
                    return NotFound("Timetable not found");
                }

                // Remove existing breaks
                _context.TimetableBreaks.RemoveRange(timetable.TimetableBreaks);
                
                // Add new breaks
                foreach (var breakInfo in request.Breaks)
                {
                    _context.TimetableBreaks.Add(new TimetableBreak
                    {
                        TimetableId = request.TimetableId,
                        Title = breakInfo.Title,
                        AfterPeriod = breakInfo.AfterPeriod,
                        Duration = breakInfo.Duration
                    });
                }
                
                await _context.SaveChangesAsync();

                // Regenerate slots with new break configuration
                await RegenerateTimetableSlots(request.TimetableId);

                return Ok(new { success = true, message = "Breaks configuration updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating breaks configuration: {ex.Message}");
            }
        }

        public class MultipleBreaksRequest
        {
            public int TimetableId { get; set; }
            public List<BreakInfo> Breaks { get; set; }
        }

        public class BreakInfo
        {
            public string Title { get; set; }
            public int AfterPeriod { get; set; }
            public int Duration { get; set; }
        }

        public class SlotUpdateRequest
        {
            public int SlotId { get; set; }
            public int? TeacherAssignmentId { get; set; }
        }

        // GET: Timetables/SchoolTimetable
        public async Task<IActionResult> SchoolTimetable()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isTeacher = User.IsInRole("Teacher");

            IQueryable<Timetable> timetablesQuery = _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(t => t.TimetableBreaks)
                .Where(t => t.IsActive);

            // Filter timetables for teachers - show only classes they teach
            if (isTeacher && currentUser.EmployeeId.HasValue)
            {
                // First get the teacher's class-section combinations
                var teacherClassSections = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == currentUser.EmployeeId.Value && ta.IsActive)
                    .Select(ta => new { ta.ClassId, ta.SectionId })
                    .Distinct()
                    .ToListAsync();

                // Get all timetables first, then filter in memory
                var allTimetables = await timetablesQuery.ToListAsync();
                
                // Filter to only timetables matching teacher's assignments
                var filteredTimetables = allTimetables
                    .Where(t => teacherClassSections.Any(cs => cs.ClassId == t.ClassId && cs.SectionId == t.SectionId))
                    .ToList();

                // Order each timetable's slots
                foreach (var timetable in filteredTimetables)
                {
                    timetable.TimetableSlots = timetable.TimetableSlots
                        .OrderBy(ts => ts.DayOfWeek)
                        .ThenBy(ts => ts.PeriodNumber)
                        .ToList();
                }

                return View(filteredTimetables);
            }

            var timetables = await timetablesQuery.ToListAsync();

            // Order each timetable's slots
            foreach (var timetable in timetables)
            {
                timetable.TimetableSlots = timetable.TimetableSlots
                    .OrderBy(ts => ts.DayOfWeek)
                    .ThenBy(ts => ts.PeriodNumber)
                    .ToList();
            }

            return View(timetables);
        }

        // Helper method to generate timetable slots
        private async Task GenerateTimetableSlots(int timetableId)
        {
            var timetable = await _context.Timetables
                .Include(t => t.TimetableBreaks)
                .FirstOrDefaultAsync(t => t.Id == timetableId);
            if (timetable == null) return;

            var slots = new List<TimetableSlot>();

            // Generate slots for each day (Monday to Friday)
            for (int day = 1; day <= 5; day++)
            {
                var currentTime = timetable.StartTime;

                // Add zero period if exists
                if (timetable.ZeroPeriodDuration > 0 && timetable.ZeroPeriodStartTime.HasValue)
                {
                    slots.Add(new TimetableSlot
                    {
                        TimetableId = timetableId,
                        DayOfWeek = day,
                        PeriodNumber = ZERO_PERIOD_NUMBER,
                        StartTime = timetable.ZeroPeriodStartTime.Value,
                        EndTime = timetable.ZeroPeriodStartTime.Value.AddMinutes(timetable.ZeroPeriodDuration),
                        IsZeroPeriod = true,
                        CustomTitle = "Zero Period"
                    });
                }

                // Get breaks for this timetable ordered by period
                var breaks = timetable.TimetableBreaks?.OrderBy(b => b.AfterPeriod).ToList() ?? new List<TimetableBreak>();
                
                // Add regular periods and breaks
                for (int period = 1; period <= timetable.NumberOfLectures; period++)
                {
                    // Check if there's a break after the previous period
                    var breakAfterPreviousPeriod = breaks.FirstOrDefault(b => b.AfterPeriod == period - 1);
                    if (breakAfterPreviousPeriod != null)
                    {
                        slots.Add(new TimetableSlot
                        {
                            TimetableId = timetableId,
                            DayOfWeek = day,
                            PeriodNumber = BREAK_PERIOD_BASE + breaks.IndexOf(breakAfterPreviousPeriod), // Unique period number for each break
                            StartTime = currentTime,
                            EndTime = currentTime.AddMinutes(breakAfterPreviousPeriod.Duration),
                            IsBreak = true,
                            CustomTitle = breakAfterPreviousPeriod.Title
                        });
                        currentTime = currentTime.AddMinutes(breakAfterPreviousPeriod.Duration);
                    }
                    // Fallback to legacy break system if no TimetableBreaks configured
                    else if (period == timetable.BreakAfterPeriod + 1 && timetable.BreakDuration > 0 && !breaks.Any())
                    {
                        slots.Add(new TimetableSlot
                        {
                            TimetableId = timetableId,
                            DayOfWeek = day,
                            PeriodNumber = BREAK_PERIOD_BASE, // Special period number for break
                            StartTime = currentTime,
                            EndTime = currentTime.AddMinutes(timetable.BreakDuration),
                            IsBreak = true,
                            CustomTitle = "Break"
                        });
                        currentTime = currentTime.AddMinutes(timetable.BreakDuration);
                    }

                    // Add regular lecture period
                    slots.Add(new TimetableSlot
                    {
                        TimetableId = timetableId,
                        DayOfWeek = day,
                        PeriodNumber = period,
                        StartTime = currentTime,
                        EndTime = currentTime.AddMinutes(timetable.LectureDuration),
                        CustomTitle = ""
                    });

                    currentTime = currentTime.AddMinutes(timetable.LectureDuration);
                }
            }

            _context.TimetableSlots.AddRange(slots);
            await _context.SaveChangesAsync();
        }

        // Helper method to regenerate slots (preserving existing assignments)
        private async Task RegenerateTimetableSlots(int timetableId)
        {
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                .Include(t => t.TimetableBreaks)
                .FirstOrDefaultAsync(t => t.Id == timetableId);

            if (timetable == null) return;

            // Get all slot IDs that will be deleted
            var deletedSlotIds = timetable.TimetableSlots.Select(ts => ts.Id).ToList();

            // Get substitutions related to these slots
            var affectedSubstitutions = await _context.Substitutions
                .Where(s => deletedSlotIds.Contains(s.TimetableSlotId) && s.IsActive)
                .ToListAsync();

            // Save existing assignments with their slot metadata
            var existingAssignments = timetable.TimetableSlots
                .Where(ts => ts.TeacherAssignmentId.HasValue && !ts.IsBreak)
                .ToDictionary(ts => new { ts.DayOfWeek, ts.PeriodNumber }, ts => ts.TeacherAssignmentId.Value);

            // Save substitution mappings (DayOfWeek, PeriodNumber) -> Substitution details
            var substitutionMappings = new Dictionary<(int, int), Substitution>();
            foreach (var sub in affectedSubstitutions)
            {
                var slot = timetable.TimetableSlots.FirstOrDefault(ts => ts.Id == sub.TimetableSlotId);
                if (slot != null)
                {
                    substitutionMappings[(slot.DayOfWeek, slot.PeriodNumber)] = sub;
                }
            }

            // Remove all existing slots
            _context.TimetableSlots.RemoveRange(timetable.TimetableSlots);
            await _context.SaveChangesAsync();

            // Generate new slots
            await GenerateTimetableSlots(timetableId);

            // Restore assignments where possible
            var newSlots = await _context.TimetableSlots
                .Where(ts => ts.TimetableId == timetableId && !ts.IsBreak)
                .ToListAsync();

            foreach (var slot in newSlots)
            {
                var key = new { slot.DayOfWeek, slot.PeriodNumber };
                if (existingAssignments.ContainsKey(key))
                {
                    slot.TeacherAssignmentId = existingAssignments[key];
                    _context.Update(slot);
                }

                // Update substitutions to reference new slot IDs
                if (substitutionMappings.TryGetValue((slot.DayOfWeek, slot.PeriodNumber), out var oldSubstitution))
                {
                    oldSubstitution.TimetableSlotId = slot.Id;
                    _context.Update(oldSubstitution);
                }
            }

            // Deactivate any substitutions that couldn't be remapped (slots no longer exist)
            var remappedSubstitutionIds = substitutionMappings.Values
                .Where(s => newSlots.Any(ns => ns.Id == s.TimetableSlotId))
                .Select(s => s.Id)
                .ToList();

            var orphanedSubstitutions = affectedSubstitutions
                .Where(s => !remappedSubstitutionIds.Contains(s.Id))
                .ToList();

            foreach (var orphanedSub in orphanedSubstitutions)
            {
                orphanedSub.IsActive = false;
                _context.Update(orphanedSub);
            }

            await _context.SaveChangesAsync();
        }

        private bool TimetableExists(int id)
        {
            return _context.Timetables.Any(e => e.Id == id);
        }

        // GET: Timetables/RunningTimeTable
        public async Task<IActionResult> RunningTimeTable(DateTime? date)
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Default to today
            var selectedDate = date ?? DateTime.Today;
            ViewData["SelectedDate"] = selectedDate.ToString("yyyy-MM-dd");

            // Get day of week (1 = Monday, 5 = Friday)
            int dayOfWeek = (int)selectedDate.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7

            // Get all timetables with their slots and teacher assignments
            var timetablesQuery = _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots.Where(ts => ts.DayOfWeek == dayOfWeek && !ts.IsBreak && !ts.IsZeroPeriod))
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(t => t.TimetableSlots.Where(ts => ts.DayOfWeek == dayOfWeek && !ts.IsBreak && !ts.IsZeroPeriod))
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Where(t => t.IsActive);

            // Filter by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                timetablesQuery = timetablesQuery.Where(t => t.CampusId == userCampusId.Value);
            }

            var timetables = await timetablesQuery.ToListAsync();

            // Get all substitutions for the selected date
            var substitutionsQuery = _context.Substitutions
                .Include(s => s.TimetableSlot)
                .Include(s => s.SubstituteEmployee)
                .Where(s => s.Date.Date == selectedDate.Date && s.IsActive);

            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                substitutionsQuery = substitutionsQuery.Where(s => s.CampusId == userCampusId.Value);
            }

            var substitutions = await substitutionsQuery.ToListAsync();

            // Create a dictionary for quick substitution lookup
            var substitutionsBySlot = substitutions.ToDictionary(s => s.TimetableSlotId, s => s);

            // Group timetables by grade level
            var groupedTimetables = timetables
                .GroupBy(t => t.Class.GradeLevel ?? "Ungrouped")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Class.Name).ThenBy(t => t.Section.Name).ToList());

            ViewData["GroupedTimetables"] = groupedTimetables;
            ViewData["SubstitutionsBySlot"] = substitutionsBySlot;

            return View();
        }
    }
}