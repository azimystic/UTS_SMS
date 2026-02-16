using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Controllers
{
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Exams
        public async Task<IActionResult> Index()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            var examsQuery = _context.Exams
                .Include(e => e.ExamCategory)
                .Include(e => e.Campus)
                .Where(e => e.IsActive);

            // Filter by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                // Show exams from user's campus OR exams with null campus (all-campus exams)
                examsQuery = examsQuery.Where(e => e.CampusId == userCampusId.Value || e.CampusId == null);
            }

            var exams = await examsQuery.ToListAsync();

            return View(exams);
        }

        // GET: Exams/Create
        public async Task<IActionResult> Create()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Filter exam categories: show user's campus categories or all-campus categories (CampusId == null)
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.ExamCategories = _context.ExamCategories
                    .Where(ec => ec.IsActive && (ec.CampusId == userCampusId.Value || ec.CampusId == null))
                    .ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.ExamCategories = _context.ExamCategories.Where(ec => ec.IsActive).ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }

            ViewBag.ShowAllCampusesOption = isOwner;
            return View();
        }

        // POST: Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ExamCategoryId,CampusId")] Exam exam)
        {
            // Get current user's campus for validation
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Validate campus selection
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                // Non-owners can only create exams for their campus
                if (exam.CampusId != userCampusId.Value && exam.CampusId != null)
                {
                    ModelState.AddModelError("CampusId", "You can only create exams for your campus.");
                }
            }

            ModelState.Remove("Campus");
            ModelState.Remove("ExamCategory");
            if (ModelState.IsValid)
            {
                exam.IsActive = true;
                _context.Add(exam);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Exam created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns on error
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.ExamCategories = _context.ExamCategories
                    .Where(ec => ec.IsActive && (ec.CampusId == userCampusId.Value || ec.CampusId == null))
                    .ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.ExamCategories = _context.ExamCategories.Where(ec => ec.IsActive).ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }
            ViewBag.ShowAllCampusesOption = isOwner;
            return View(exam);
        }

        // GET: Exams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null || !exam.IsActive)
            {
                return NotFound();
            }

            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Filter dropdowns based on user role
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.ExamCategories = _context.ExamCategories
                    .Where(ec => ec.IsActive && (ec.CampusId == userCampusId.Value || ec.CampusId == null))
                    .ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.ExamCategories = _context.ExamCategories.Where(ec => ec.IsActive).ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }

            ViewBag.ShowAllCampusesOption = isOwner;
            return View(exam);
        }

        // POST: Exams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ExamCategoryId,CampusId,IsActive")] Exam exam)
        {
            if (id != exam.Id)
            {
                return NotFound();
            }

            // Get current user's campus for validation
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Validate campus selection
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                // Non-owners can only edit exams for their campus
                if (exam.CampusId != userCampusId.Value && exam.CampusId != null)
                {
                    ModelState.AddModelError("CampusId", "You can only edit exams for your campus.");
                }
            }

            ModelState.Remove("Campus");
            ModelState.Remove("ExamCategory");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Exam updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamExists(exam.Id))
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

            // Re-populate dropdowns on error
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.ExamCategories = _context.ExamCategories
                    .Where(ec => ec.IsActive && (ec.CampusId == userCampusId.Value || ec.CampusId == null))
                    .ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.ExamCategories = _context.ExamCategories.Where(ec => ec.IsActive).ToList();
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }
            ViewBag.ShowAllCampusesOption = isOwner;
            return View(exam);
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
    }
}