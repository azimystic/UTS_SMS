using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using System.Linq;

namespace UTS_SMS.Controllers
{
    // Teacher Assignment Controller
    public class TeacherAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherAssignmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        // GET: TeacherAssignments
        public async Task<IActionResult> Index(bool showInactive = false)
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            IQueryable<TeacherAssignment> assignmentsQuery = _context.TeacherAssignments
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .Include(ta => ta.Teacher); // Assuming you have a Teacher navigation property

            // Filter by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                assignmentsQuery = assignmentsQuery.Where(ta => ta.CampusId == userCampusId.Value);
            }

            // Show both active and inactive when showInactive is true, otherwise only active
            if (!showInactive)
            {
                assignmentsQuery = assignmentsQuery.Where(ta => ta.IsActive);
            }

            var assignments = await assignmentsQuery.ToListAsync();
            ViewBag.ShowInactive = showInactive;
            return View(assignments);
        }

        // GET: TeacherAssignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherAssignment = await _context.TeacherAssignments
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .Include(ta => ta.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (teacherAssignment == null)
            {
                return NotFound();
            }

            return View(teacherAssignment);
        }

        // GET: TeacherAssignments/Create
        public async Task<IActionResult> Create()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Filter dropdowns by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive && c.CampusId == userCampusId.Value), "Id", "Name");
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && cs.CampusId == userCampusId.Value), "Id", "Name");
                ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive && s.CampusId == userCampusId.Value), "Id", "Name");
                ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher" && t.CampusId == userCampusId.Value), "Id", "FullName");
            }
            else
            {
                // Owner sees all campuses
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive), "Id", "Name");
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive), "Id", "Name");
                ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive), "Id", "Name");
                ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher"), "Id", "FullName");
            }

            return View();
        }

        // POST: TeacherAssignments/Create - Dynamic bulk create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int teacherId, int subjectId, List<int> classIds, List<int> sectionIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            if (classIds == null || sectionIds == null || classIds.Count != sectionIds.Count || classIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Invalid class and section data.";
                return RedirectToAction(nameof(Create));
            }

            ModelState.Remove("Campus");
            
            var createdCount = 0;
            var reactivatedCount = 0;
            var skippedCount = 0;

            for (int i = 0; i < classIds.Count; i++)
            {
                var classId = classIds[i];
                var sectionId = sectionIds[i];

                // Check if this exact combination exists
                var existingAssignment = await _context.TeacherAssignments
                    .FirstOrDefaultAsync(ta =>
                        ta.TeacherId == teacherId &&
                        ta.SubjectId == subjectId &&
                        ta.ClassId == classId &&
                        ta.SectionId == sectionId);

                if (existingAssignment != null)
                {
                    if (!existingAssignment.IsActive)
                    {
                        // Reactivate inactive assignment
                        existingAssignment.IsActive = true;
                        _context.Update(existingAssignment);
                        reactivatedCount++;
                    }
                    else
                    {
                        // Already exists and active, skip
                        skippedCount++;
                    }
                }
                else
                {
                    // Create new assignment
                    var newAssignment = new TeacherAssignment
                    {
                        TeacherId = teacherId,
                        SubjectId = subjectId,
                        ClassId = classId,
                        SectionId = sectionId,
                        CampusId = (int)campusId,
                        IsActive = true
                    };
                    _context.Add(newAssignment);
                    createdCount++;
                }
            }

            await _context.SaveChangesAsync();

            var message = $"Successfully created {createdCount} assignment(s)";
            if (reactivatedCount > 0) message += $", reactivated {reactivatedCount}";
            if (skippedCount > 0) message += $", skipped {skippedCount} duplicate(s)";
            message += ".";

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // GET: TeacherAssignments/Edit/5
        public async Task<IActionResult> Edit(int? id, int? teacherId, int? subjectId)
        {
            // Handle bulk edit by Teacher and Subject
            if (teacherId.HasValue && subjectId.HasValue)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userCampusId = currentUser?.CampusId;
                var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

                // Get all assignments for this Teacher/Subject combination
                var assignments = await _context.TeacherAssignments
                    .Include(ta => ta.Class)
                    .Include(ta => ta.Section)
                    .Include(ta => ta.Subject)
                    .Include(ta => ta.Teacher)
                    .Where(ta => ta.TeacherId == teacherId.Value && 
                                 ta.SubjectId == subjectId.Value && 
                                 ta.IsActive)
                    .ToListAsync();

                // Filter dropdowns by campus for non-owner users
                if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
                {
                    ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive && c.CampusId == userCampusId.Value), "Id", "Name");
                    ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher" && t.CampusId == userCampusId.Value), "Id", "FullName", teacherId);
                    ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive && s.CampusId == userCampusId.Value), "Id", "Name", subjectId);
                }
                else
                {
                    ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive), "Id", "Name");
                    ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher"), "Id", "FullName", teacherId);
                    ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive), "Id", "Name", subjectId);
                }

                ViewBag.TeacherId = teacherId.Value;
                ViewBag.SubjectId = subjectId.Value;
                ViewBag.IsBulkEdit = true;
                
                return View("EditBulk", assignments);
            }

            // Handle single assignment edit
            if (id == null)
            {
                return NotFound();
            }

            var teacherAssignment = await _context.TeacherAssignments
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (teacherAssignment == null)
            {
                return NotFound();
            }

            // Get current user's campus
            var currentUser2 = await _userManager.GetUserAsync(User);
            var userCampusId2 = currentUser2?.CampusId;
            var isOwner2 = User.IsInRole("Owner") || userCampusId2 == null || userCampusId2 == 0;

            // Filter dropdowns by campus for non-owner users
            if (!isOwner2 && userCampusId2.HasValue && userCampusId2.Value != 0)
            {
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive && c.CampusId == userCampusId2.Value), "Id", "Name", teacherAssignment.ClassId);
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && cs.CampusId == userCampusId2.Value), "Id", "Name", teacherAssignment.SectionId);
                ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive && s.CampusId == userCampusId2.Value), "Id", "Name", teacherAssignment.SubjectId);
                ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher" && t.CampusId == userCampusId2.Value), "Id", "FullName", teacherAssignment.TeacherId);
            }
            else
            {
                // Owner sees all campuses
                ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive), "Id", "Name", teacherAssignment.ClassId);
                ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive), "Id", "Name", teacherAssignment.SectionId);
                ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive), "Id", "Name", teacherAssignment.SubjectId);
                ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher"), "Id", "FullName", teacherAssignment.TeacherId);
            }

            return View(teacherAssignment);
        }

        // POST: TeacherAssignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeacherAssignment teacherAssignment)
        {
            if (id != teacherAssignment.Id)
            {
                return NotFound();
            }

            // Check if this subject-class-section combination already has a teacher (excluding current assignment)
            var existingAssignment = await _context.TeacherAssignments
                .FirstOrDefaultAsync(ta =>
                    ta.SubjectId == teacherAssignment.SubjectId &&
                    ta.ClassId == teacherAssignment.ClassId &&
                    ta.SectionId == teacherAssignment.SectionId &&
                    ta.Id != teacherAssignment.Id &&
                    ta.IsActive  );

            if (existingAssignment != null)
            {
                ModelState.AddModelError("", "This subject-class-section combination already has an assigned teacher.");
            }
            ModelState.Remove("Campus");
            ModelState.Remove("Section");
            ModelState.Remove("Subject");
            ModelState.Remove("Teacher");
            ModelState.Remove("Class");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacherAssignment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherAssignmentExists(teacherAssignment.Id))
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

            // Get current user's campus for dropdown filtering on validation error
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            ViewData["ClassId"] = new SelectList(_context.Classes.Where(c => c.IsActive && (userCampusId == null || c.CampusId == userCampusId)), "Id", "Name", teacherAssignment.ClassId);
            ViewData["SectionId"] = new SelectList(_context.ClassSections.Where(cs => cs.IsActive && (userCampusId == null || cs.CampusId == userCampusId)), "Id", "Name", teacherAssignment.SectionId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects.Where(s => s.IsActive && (userCampusId == null || s.CampusId == userCampusId)), "Id", "Name", teacherAssignment.SubjectId);
            ViewData["TeacherId"] = new SelectList(_context.Employees.Where(t => t.IsActive && t.Role == "Teacher" && (userCampusId == null || t.CampusId == userCampusId)), "Id", "FullName", teacherAssignment.TeacherId);
            return View(teacherAssignment);
        }

        // POST: TeacherAssignments/EditBulk - Handle bulk edit for Teacher/Subject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBulk(int teacherId, int subjectId, List<int> assignmentIds, List<int> classIds, List<int> sectionIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            if (classIds == null || sectionIds == null || classIds.Count == 0 || sectionIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Please add at least one class and section combination.";
                return RedirectToAction(nameof(Index));
            }

            if (assignmentIds == null)
            {
                assignmentIds = new List<int>();
            }

            var updatedCount = 0;
            var createdCount = 0;
            var deactivatedCount = 0;

            // Track which existing assignments are in the form (to deactivate those not included)
            var existingAssignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.SubjectId == subjectId && ta.IsActive)
                .ToListAsync();

            var processedIds = new List<int>();

            // Process submitted rows
            // Process submitted rows
            for (int i = 0; i < classIds.Count; i++)
            {
                var classId = classIds[i];
                var sectionId = sectionIds[i];
                var assignmentId = i < assignmentIds.Count ? assignmentIds[i] : 0;

                if (assignmentId > 0)
                {
                    // 1. Existing Assignment passed from the form
                    var assignment = await _context.TeacherAssignments.FindAsync(assignmentId);
                    if (assignment != null)
                    {
                        assignment.ClassId = classId;
                        assignment.SectionId = sectionId;
                        assignment.IsActive = true; // Ensure it's active
                        _context.Update(assignment);
                        updatedCount++;
                        processedIds.Add(assignmentId);
                    }
                }
                else
                {
                    // 2. New row from form: Check if an INACTIVE one already exists in DB to reactivate
                    var existingInactive = await _context.TeacherAssignments
                        .FirstOrDefaultAsync(ta => ta.TeacherId == teacherId
                                                && ta.SubjectId == subjectId
                                                && ta.ClassId == classId
                                                && ta.SectionId == sectionId
                                                && !ta.IsActive);

                    if (existingInactive != null)
                    {
                        // Reactivate the existing record
                        existingInactive.IsActive = true;
                        _context.Update(existingInactive);
                        updatedCount++; // Count as updated/reactivated
                        processedIds.Add(existingInactive.Id);
                    }
                    else
                    {
                        // 3. Truly new assignment: Create from scratch
                        var newAssignment = new TeacherAssignment
                        {
                            TeacherId = teacherId,
                            SubjectId = subjectId,
                            ClassId = classId,
                            SectionId = sectionId,
                            CampusId = (int)campusId,
                            IsActive = true
                        };
                        _context.Add(newAssignment);
                        createdCount++;
                        // We don't need to add to processedIds because it wasn't in existingAssignments list
                    }
                }
            }

            // Deactivate assignments that were removed from the form
            foreach (var existingAssignment in existingAssignments)
            {
                if (!processedIds.Contains(existingAssignment.Id))
                {
                    existingAssignment.IsActive = false;
                    _context.Update(existingAssignment);
                    deactivatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            var message = $"Successfully updated {updatedCount} assignment(s)";
            if (createdCount > 0) message += $", created {createdCount}";
            if (deactivatedCount > 0) message += $", deactivated {deactivatedCount}";
            message += ".";

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // POST: TeacherAssignments/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var teacherAssignment = await _context.TeacherAssignments.FindAsync(id);
            if (teacherAssignment == null)
            {
                return NotFound();
            }

            teacherAssignment.IsActive = !teacherAssignment.IsActive;
            _context.Update(teacherAssignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Assignment {(teacherAssignment.IsActive ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: TeacherAssignments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherAssignment = await _context.TeacherAssignments
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .Include(ta => ta.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (teacherAssignment == null)
            {
                return NotFound();
            }

            return View(teacherAssignment);
        }

        // POST: TeacherAssignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacherAssignment = await _context.TeacherAssignments.FindAsync(id);
            if (teacherAssignment != null)
            {
                teacherAssignment.IsActive = false;
                _context.Update(teacherAssignment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: TeacherAssignments/GetSectionsByClass/5
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(cs => cs.ClassId == classId && cs.IsActive)
                .Select(cs => new { cs.Id, cs.Name })
                .ToListAsync();

            return Json(sections);
        }

        private bool TeacherAssignmentExists(int id)
        {
            return _context.TeacherAssignments.Any(e => e.Id == id && e.IsActive);
        }

        // POST: TeacherAssignments/DeactivateAllForTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAllForTeacher(int teacherId)
        {
            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully deactivated {assignments.Count} assignment(s) for the teacher.";
            return RedirectToAction(nameof(Index));
        }

        // POST: TeacherAssignments/DeactivateAllForSubject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAllForSubject(int subjectId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            IQueryable<TeacherAssignment> assignmentsQuery = _context.TeacherAssignments
                .Where(ta => ta.SubjectId == subjectId && ta.IsActive);

            // Filter by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                assignmentsQuery = assignmentsQuery.Where(ta => ta.CampusId == userCampusId.Value);
            }

            var assignments = await assignmentsQuery.ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully deactivated {assignments.Count} assignment(s) for the subject.";
            return RedirectToAction(nameof(Index));
        }
    }
}
