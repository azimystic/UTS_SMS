using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
 using UTS_SMS.Models;
using UTS_SMS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UTS_SMS.Controllers
{
    public class SubjectsGroupingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectsGroupingsController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
        }

        // GET: SubjectsGroupings
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;
            var subjectsGroupings = await _context.SubjectsGroupings
                .Include(sg => sg.Campus)
                 .Where(sg => sg.IsActive)
                .ToListAsync();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                subjectsGroupings = subjectsGroupings.Where(c => c.CampusId == userCampusId.Value).ToList();
            }
            return View(subjectsGroupings);
        }

        // GET: SubjectsGroupings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subjectsGrouping = await _context.SubjectsGroupings
                .Include(sg => sg.Campus)
                .Include(sg => sg.SubjectsGroupingDetails)
                    .ThenInclude(sgd => sgd.Subject)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (subjectsGrouping == null)
            {
                return NotFound();
            }

            return View(subjectsGrouping);
        }

        // GET: SubjectsGroupings/Create
        public IActionResult Create()
        {
            var currentUserTask = _userManager.GetUserAsync(User);
            currentUserTask.Wait();
            var currentUser = currentUserTask.Result;
            var userCampusId = currentUser?.CampusId;

            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                ViewData["Campuses"] = _context.Campuses
                    .Where(c => c.IsActive && c.Id == userCampusId)
                    .ToList(); // return List<Campus>
            }
            else
            {
                ViewData["Campuses"] = _context.Campuses
                    .Where(c => c.IsActive)
                    .ToList();
            }

            return View();

            
        }

        // POST: SubjectsGroupings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CampusId")] SubjectsGrouping subjectsGrouping)
        {
            ModelState.Remove("Students");
            ModelState.Remove("Campus");
            ModelState.Remove("SubjectsGroupingDetails");
            if (ModelState.IsValid)
            {
                subjectsGrouping.IsActive = true;
                _context.Add(subjectsGrouping);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Subjects group created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Campuses"] = _context.Campuses.Where(c => c.IsActive).ToList();
            return View(subjectsGrouping);
        }

        // GET: SubjectsGroupings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subjectsGrouping = await _context.SubjectsGroupings
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (subjectsGrouping == null)
            {
                return NotFound();
            }

            ViewData["Campuses"] = _context.Campuses.Where(c => c.IsActive).ToList();
            return View(subjectsGrouping);
        }

        // POST: SubjectsGroupings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CampusId,IsActive")] SubjectsGrouping subjectsGrouping)
        {
            if (id != subjectsGrouping.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Students");
            ModelState.Remove("Campus");
            ModelState.Remove("SubjectsGroupingDetails");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subjectsGrouping);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Subjects group updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectsGroupingExists(subjectsGrouping.Id))
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

            ViewData["Campuses"] = _context.Campuses.Where(c => c.IsActive).ToList();
            return View(subjectsGrouping);
        }

        // GET: SubjectsGroupings/ManageSubjects/5
        public async Task<IActionResult> ManageSubjects(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subjectsGrouping = await _context.SubjectsGroupings
                .Include(sg => sg.SubjectsGroupingDetails)
                    .ThenInclude(sgd => sgd.Subject)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (subjectsGrouping == null)
            {
                return NotFound();
            }

            // Get all active subjects
            var allSubjects = await _context.Subjects
                .Where(s => s.IsActive)
                .ToListAsync();

            // Get already assigned subject IDs
            var assignedSubjectIds = subjectsGrouping.SubjectsGroupingDetails
                .Where(sgd => sgd.IsActive)
                .Select(sgd => sgd.SubjectId)
                .ToList();

            ViewData["AllSubjects"] = allSubjects;
            ViewData["AssignedSubjectIds"] = assignedSubjectIds;

            return View(subjectsGrouping);
        }

        // POST: SubjectsGroupings/ManageSubjects/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageSubjects(int id, [FromForm] List<int> selectedSubjects)
        {
            var subjectsGrouping = await _context.SubjectsGroupings
                .Include(sg => sg.SubjectsGroupingDetails)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (subjectsGrouping == null)
            {
                return NotFound();
            }

            try
            {
                // Get current active subject assignments
                var currentAssignments = subjectsGrouping.SubjectsGroupingDetails
                    .Where(sgd => sgd.IsActive)
                    .ToList();

                // Remove assignments that are no longer selected
                foreach (var assignment in currentAssignments)
                {
                    if (!selectedSubjects.Contains(assignment.SubjectId))
                    {
                         _context.Remove(assignment);
                    }
                }

                // Add new assignments
                foreach (var subjectId in selectedSubjects)
                {
                    // Check if this subject is already assigned and active
                    var existingAssignment = currentAssignments
                        .FirstOrDefault(sgd => sgd.SubjectId == subjectId && sgd.IsActive);

                    if (existingAssignment == null)
                    {
                        // Check if this subject exists and is active
                        var subject = await _context.Subjects
                            .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

                        if (subject != null)
                        {
                            // Create new assignment
                            var newAssignment = new SubjectsGroupingDetails
                            {
                                SubjectId = subjectId,
                                SubjectsGroupingId = id,
                                IsActive = true
                            };

                            _context.Add(newAssignment);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Subjects updated successfully for this group!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating subjects: {ex.Message}");

                // Reload view data for the form
                var allSubjects = await _context.Subjects
                    .Where(s => s.IsActive)
                    .ToListAsync();

                ViewData["AllSubjects"] = allSubjects;
                ViewData["AssignedSubjectIds"] = selectedSubjects;

                return View(subjectsGrouping);
            }
        }

        // GET: SubjectsGroupings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subjectsGrouping = await _context.SubjectsGroupings
                .Include(sg => sg.Campus)
                 .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (subjectsGrouping == null)
            {
                return NotFound();
            }

           

            return View(subjectsGrouping);
        }

        // POST: SubjectsGroupings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subjectsGrouping = await _context.SubjectsGroupings
                 .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (subjectsGrouping == null)
            {
                return NotFound();
            }

            // Check if group has students assigned
            

            // Soft delete - set IsActive to false
            subjectsGrouping.IsActive = false;

            // Also deactivate all subject assignments
            var subjectAssignments = await _context.SubjectsGroupingDetails
                .Where(sgd => sgd.SubjectsGroupingId == id && sgd.IsActive)
                .ToListAsync();

            foreach (var assignment in subjectAssignments)
            {
                assignment.IsActive = false;
                _context.Update(assignment);
            }

            _context.Update(subjectsGrouping);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Subjects group deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

      

        private bool SubjectsGroupingExists(int id)
        {
            return _context.SubjectsGroupings.Any(e => e.Id == id && e.IsActive);
        }
    }
}