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
    public class ExamCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamCategoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ExamCategories
        public async Task<IActionResult> Index()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            var examCategoriesQuery = _context.ExamCategories
                .Include(ec => ec.Campus)
                .Where(ec => ec.IsActive);

            // Filter by campus for non-owner users
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                // Show categories from user's campus OR categories with null campus (all-campus categories)
                examCategoriesQuery = examCategoriesQuery.Where(ec => ec.CampusId == userCampusId.Value || ec.CampusId == null);
            }

            var examCategories = await examCategoriesQuery.ToListAsync();

            return View(examCategories);
        }

        // GET: ExamCategories/Create
        public async Task<IActionResult> Create()
        {
            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }

            ViewBag.ShowAllCampusesOption = isOwner;
            return View();
        }

        // POST: ExamCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CampusId")] ExamCategory examCategory)
        {
            // Get current user's campus for validation
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            // Validate campus selection
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                // Non-owners can only create categories for their campus
                if (examCategory.CampusId != userCampusId.Value && examCategory.CampusId != null)
                {
                    ModelState.AddModelError("CampusId", "You can only create exam categories for your campus.");
                }
            }

            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                examCategory.IsActive = true;
                _context.Add(examCategory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Exam category created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns on error
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }
            ViewBag.ShowAllCampusesOption = isOwner;
            return View(examCategory);
        }

        // GET: ExamCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var examCategory = await _context.ExamCategories.FindAsync(id);
            if (examCategory == null || !examCategory.IsActive)
            {
                return NotFound();
            }

            // Get current user's campus
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner") || userCampusId == null || userCampusId == 0;

            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }

            ViewBag.ShowAllCampusesOption = isOwner;
            return View(examCategory);
        }

        // POST: ExamCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CampusId,IsActive")] ExamCategory examCategory)
        {
            if (id != examCategory.Id)
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
                // Non-owners can only edit categories for their campus
                if (examCategory.CampusId != userCampusId.Value && examCategory.CampusId != null)
                {
                    ModelState.AddModelError("CampusId", "You can only edit exam categories for your campus.");
                }
            }

            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(examCategory);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Exam category updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamCategoryExists(examCategory.Id))
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
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive && c.Id == userCampusId.Value).ToList();
            }
            else
            {
                ViewBag.Campuses = _context.Campuses.Where(c => c.IsActive).ToList();
            }
            ViewBag.ShowAllCampusesOption = isOwner;
            return View(examCategory);
        }

        private bool ExamCategoryExists(int id)
        {
            return _context.ExamCategories.Any(e => e.Id == id);
        }
    }
}